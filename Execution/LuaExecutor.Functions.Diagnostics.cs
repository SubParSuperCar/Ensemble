using System.Diagnostics;
using System.Globalization;
using System.Text;
using EnsembleRoot.Common.Logging;
using EnsembleRoot.Common.Utils;
using Godot;
using Lua;
using Serilog;
using Environment = System.Environment;

// ReSharper disable InconsistentNaming

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	private static ValueTask<int> clr_log(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		// Deferred so the Lua executor return log message is cleared too
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
		var functions = Functions.Select(static entry => $"-> {entry.Function.Name}({entry.Parameters})");

		Log.Information(
			"Custom functions (\"?\" = optional, \"|\" = or, \"loc_\" = local only):\n{Functions}",
			string.Join('\n', functions));

		return context.ReturnNothing();
	}

	private static ValueTask<int> print(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		Log.Information("Lua: \"{Message}\"", string.Join(' ', [.. context.Arguments]));

		return context.ReturnNothing();
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
				LuaValueType.String => $"\"{EscapeString(luaValue.Read<string>())}\"",
				_ => luaValue.ToString()
			};

			lines.Append(childPath).Append(" = ").Append(value).Append('\n');

			if (isNewTable)
				DumpTable(lines, childTable!, childPath, visited);
		}
	}

	private static string EscapeString(string value) =>
		value
			.Replace("\\", @"\\", StringComparison.Ordinal)
			.Replace("\"", "\\\"", StringComparison.Ordinal)
			.Replace("\r", "\\r", StringComparison.Ordinal)
			.Replace("\n", "\\n", StringComparison.Ordinal);
}
