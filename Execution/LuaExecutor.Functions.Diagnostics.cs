using System.Diagnostics;
using System.Globalization;
using System.Text;
using EnsembleRoot.Common.Logging;
using EnsembleRoot.Common.Utils;
using EnsembleRoot.Scripts.Watchdog;
using Godot;
using Lua;
using Serilog;
using Environment = System.Environment;

// ReSharper disable InconsistentNaming

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	private const double DefaultTraceSeconds = 5d;
	private const double MaxTraceSeconds = 60d;

	// Of each frame's time, beyond which frames mostly wait rather than work
	private const double WaitingShare = 1 / 2d;

	private static ValueTask<int> clr_log(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		// Deferred, so the run's own result line is cleared too
		Callable.From(VolatileLogHistorySink.Clear).CallDeferred();

		return context.ReturnNothing();
	}

	private static ValueTask<int> dmp_asm_info(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var assemblyNames = AppDomain.CurrentDomain.GetAssemblies()
			.Select(static assembly => assembly.GetName())
			.OrderBy(static name => name.Name, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		Log.Information(
			"Game: v{Version} (Commit={Commit}, BuildTime={BuildTime}, Compilation={Compilation}, Runtime={Runtime})",
			GameVersion, GameCommit, BuildInfo.BuildTime, BuildInfo.IsNativeAot ? "AOT" : "JIT", Environment.Version);

		Log.Information(
			"Loaded assemblies ({Count}):\n{Assemblies}",
			assemblyNames.Length, string.Join('\n', assemblyNames.Select(static name => $"~~> {name}")));

		return context.ReturnNothing();
	}

	private static ValueTask<int> dmp_env(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var lines = new StringBuilder();
		DumpTable(lines, context.State.Environment, "_ENV", []);

		Log.Information("Contents of _ENV:\n{Contents}", lines.ToString().TrimEnd());

		return context.ReturnNothing();
	}

	private static ValueTask<int> dmp_inp_map(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var actions = InputMap.GetActions().Select(static action => action.ToString()).Order(StringComparer.Ordinal);
		var lines = new StringBuilder();

		foreach (var action in actions)
		{
			lines.Append(action).Append(":\n");

			var index = 1;
			foreach (var @event in InputMap.ActionGetEvents(action))
				lines.Append(CultureInfo.InvariantCulture, $"{index++}. {@event.AsText()}\n");
		}

		Log.Information("Contents of InputMap:\n{Contents}", lines.ToString().TrimEnd());

		return context.ReturnNothing();
	}

	private static ValueTask<int> gc(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var before = GC.GetTotalMemory(false);

		Log.Information("GC heap size before: {BytesBefore}", ByteFormat.Humanize((ulong)before));
		var stopwatch = Stopwatch.StartNew();

		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
		GC.WaitForPendingFinalizers();
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

		stopwatch.Stop();
		var after = GC.GetTotalMemory(false);
		var reclaimed = Math.Max(0, before - after);

		Log.Information(
			"GC heap size after: {BytesAfter} (reclaimed {BytesReclaimed} in {ElapsedMs:F3} ms)",
			ByteFormat.Humanize((ulong)after), ByteFormat.Humanize((ulong)reclaimed),
			stopwatch.Elapsed.TotalMilliseconds);

		return context.ReturnNothing();
	}

	private static ValueTask<int> help(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var query = context.GetArgumentOrDefault(0, string.Empty);
		var matches = Functions
			.Where(entry => entry.Function.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
			.ToArray();

		if (context.GetArgumentOrDefault(1, false))
		{
			Log.Information(
				"Custom functions ({Count}): {Names}", matches.Length,
				string.Join(", ", matches.Select(static entry => entry.Function.Name)));

			return context.ReturnNothing();
		}

		var functions = matches.Select(static entry =>
			$"-> {entry.Function.Name}({entry.Parameters}): {entry.Description}");

		Log.Information(
			"Custom functions (\"?\" = optional, \"|\" = or, \"{{...}}\" = options table, \"loc_\" = local only, " +
			"player = peer ID, player ID, or name):\n{Functions}",
			string.Join('\n', functions));

		return context.ReturnNothing();
	}

	private static ValueTask<int> print(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var values = context.Arguments.ToArray().Select(static value => LuaValueFormatter.Format(value));
		Log.Information("Lua: \"{Message}\"", string.Join(' ', values));

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_watchdog_on(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var isOn = context.GetArgument<bool>(0);

		// Holds are told apart by reference, and each call brings its own copy of the token
		var token = string.Intern(context.GetArgument<string>(1));
		var hasChanged = isOn ? Watchdog.Suspension.Release(token) : Watchdog.Suspension.Acquire(token);

		var count = Watchdog.Suspension.Count;

		if (count is 0)
			Log.Information("Watchdog on");
		else
			Log.Information("Watchdog off, held by {Count} token(s)", count);

		return new ValueTask<int>(context.Return(hasChanged));
	}

	private static async ValueTask<int> trace_frames(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var seconds = context.GetArgumentOrDefault(0, DefaultTraceSeconds);

		if (seconds is not (> 0 and <= MaxTraceSeconds))
		{
			Log.Error("Invalid trace length: {Seconds} s (takes more than 0, up to {Max})", seconds, MaxTraceSeconds);
			return context.Return();
		}

		Log.Information("Tracing frames for {Seconds} s...", seconds);
		FrameTrace trace;

		PauseBudget();

		try
		{
			trace = await TraceFramesAsync(seconds, cancellationToken).ConfigureAwait(true);
		}
		finally
		{
			ResumeBudget();
		}

		var intervals = trace.IntervalsMs;
		var count = intervals.Count;

		Log.Information(
			"Traced {Count} frame(s): {Fps:F1} FPS, {FrameMs:F3} ms per frame (median {MedianMs:F3}, 99th " +
			"percentile {SlowMs:F3}, worst {WorstMs:F3}); on average {ProcessMs:F3} ms processing, {PhysicsMs:F3} " +
			"ms in physics, {RenderMs:F3} ms rendering on the CPU, and {GpuMs:F3} ms on the GPU; VSync {Vsync} at " +
			"{RefreshRate:F0} Hz, max FPS {MaxFps}",
			count, count / seconds, intervals.Average(), intervals[count / 2], intervals[(int)(count * 0.99)],
			intervals[^1], trace.ProcessMs, trace.PhysicsMs, trace.RenderMs, trace.GpuMs,
			DisplayServer.WindowGetVsyncMode(), DisplayServer.ScreenGetRefreshRate(), Engine.MaxFps);

		var workMs = Math.Max(trace.ProcessMs + trace.PhysicsMs + trace.RenderMs, trace.GpuMs);

		if (workMs < intervals.Average() * WaitingShare)
			Log.Information(
				"Frames spend most of their time waiting, not working: something paces them, such as VSync, a frame " +
				"cap, the compositor, or the graphics driver");

		return context.Return();
	}

	// Averages per frame, over which the GPU's trailing time evens out
	private static async Task<FrameTrace> TraceFramesAsync(double seconds, CancellationToken cancellationToken)
	{
		var intervals = new List<double>();
		var (processMs, physicsMs, renderMs, gpuMs) = (0d, 0d, 0d, 0d);
		var stopwatch = Stopwatch.StartNew();

		// The first read starts the measuring, so the frames waited for are measured
		_ = FrameTimes.RenderGpuMs;

		for (var last = stopwatch.Elapsed; stopwatch.Elapsed.TotalSeconds < seconds || intervals.Count is 0;)
		{
			await DelayAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(true);

			var now = stopwatch.Elapsed;
			intervals.Add((now - last).TotalMilliseconds);
			last = now;

			processMs += FrameTimes.ProcessMs;
			physicsMs += FrameTimes.PhysicsMs;
			renderMs += FrameTimes.RenderCpuMs;
			gpuMs += FrameTimes.RenderGpuMs;
		}

		stopwatch.Stop();
		intervals.Sort();

		var count = intervals.Count;
		return new FrameTrace(intervals, processMs / count, physicsMs / count, renderMs / count, gpuMs / count);
	}

	private static void DumpTable(StringBuilder lines, LuaTable table, string path, HashSet<LuaTable> visited)
	{
		visited.Add(table);

		foreach (var (luaKey, luaValue) in table.OrderBy(static entry => entry.Key.ToString(), StringComparer.Ordinal))
		{
			var childPath = luaKey.Type is LuaValueType.Number ? $"{path}[{luaKey}]" : $"{path}.{luaKey}";
			var childTable = luaValue.Type is LuaValueType.Table ? luaValue.Read<LuaTable>() : null;
			var isNewTable = childTable is not null && !visited.Contains(childTable);

			var value = luaValue.Type switch
			{
				LuaValueType.Table => isNewTable ? "<table>" : "<already visited>",
				LuaValueType.Function => "<function>",
				LuaValueType.UserData => "<userdata>",
				LuaValueType.Thread => "<thread>",
				LuaValueType.String => $"\"{LuaValueFormatter.Escape(luaValue.Read<string>())}\"",
				_ => luaValue.ToString()
			};

			lines.Append(childPath).Append(" = ").Append(value).Append('\n');

			if (isNewTable)
				DumpTable(lines, childTable!, childPath, visited);
		}
	}

	/// <param name="IntervalsMs">Sorted, shortest first.</param>
	private sealed record FrameTrace(
		List<double> IntervalsMs,
		double ProcessMs,
		double PhysicsMs,
		double RenderMs,
		double GpuMs);
}
