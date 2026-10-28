using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using EnsembleRoot.Common.Messages;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Scripts.World;
using Godot;
using Lua;
using Serilog;

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	private const string SkyPath = "Sky";
	private const string TerrainPath = "Terrain";
	private const string TimeOfDayPath = "Sky/TimeOfDay";
	private const string NightLightPath = "Night Light";
	private const string FlashlightPath = "Character/Flashlight";
	private const string TemporalStaticPath = "Temporal Static";

	private const int DefaultStressTestCount = 1000;
	private const double DefaultStressTestSeconds = 5d;

	private static readonly StringName Sky3DEnabledProperty = "sky3d_enabled";
	private static readonly StringName GameTimeEnabledProperty = "game_time_enabled";
	private static readonly StringName SystemSyncProperty = "system_sync";
	private static readonly StringName CurrentTimeProperty = "current_time";

	private static ValueTask<int> loc_add_rand_insts(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (FindDirectPlot(context.GetArgument<int>(0), nameof(loc_add_rand_insts)) is not { } plot)
			return context.ReturnNothing();

		var count = context.GetArgument<int>(1);

		if (!IsValidInstanceCount(plot, count))
			return context.ReturnNothing();

		var stopwatch = Stopwatch.StartNew();
		AddRandomInstances(plot, count);

		stopwatch.Stop();
		Log.Information(
			"Added {Count} instance(s) to plot {PlotId} in {ElapsedMs:F3} ms",
			count, plot.Id, stopwatch.Elapsed.TotalMilliseconds);

		return context.ReturnNothing();
	}

	private static ValueTask<int> loc_clr_insts(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (FindDirectPlot(context.GetArgument<int>(0), nameof(loc_clr_insts)) is not { } plot)
			return context.ReturnNothing();

		var instances = plot.Instances;
		var count = instances.Count;

		var stopwatch = Stopwatch.StartNew();
		instances.Clear();

		stopwatch.Stop();
		Log.Information(
			"Removed {Count} instance(s) from plot {PlotId} in {ElapsedMs:F3} ms (deferred)",
			count, plot.Id, stopwatch.Elapsed.TotalMilliseconds);

		return context.ReturnNothing();
	}

	private static async ValueTask<int> loc_stress_test(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (FindDirectPlot(context.GetArgument<int>(0), nameof(loc_stress_test)) is not { } plot)
			return context.Return();

		var count = context.GetArgumentOrDefault(1, DefaultStressTestCount);
		var seconds = context.GetArgumentOrDefault(2, DefaultStressTestSeconds);

		if (!IsValidInstanceCount(plot, count))
			return context.Return();

		var frameTimesMs = new List<double>();
		var instanceIds = new List<int>();

		Log.Information(
			"Stress testing plot {PlotId} with {Count} instance(s) for {Seconds} s...", plot.Id, count, seconds);
		var start = Stopwatch.GetTimestamp();

		try
		{
			AddRandomInstances(plot, count, instanceIds);
			var addMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

			for (var last = Stopwatch.GetTimestamp(); Stopwatch.GetElapsedTime(start).TotalSeconds < seconds;)
			{
				await DelayAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(true);

				var now = Stopwatch.GetTimestamp();
				frameTimesMs.Add(Stopwatch.GetElapsedTime(last, now).TotalMilliseconds);
				last = now;
			}

			var averageMs = frameTimesMs.Count is 0 ? 0d : frameTimesMs.Average();
			var worstMs = frameTimesMs.Count is 0 ? 0d : frameTimesMs.Max();

			Log.Information(
				"Stress test added {Count} instance(s) in {AddMs:F3} ms, then averaged {AverageMs:F3} ms per frame " +
				"(worst {WorstMs:F3} ms) over {FrameCount} frame(s)",
				count, addMs, averageMs, worstMs, frameTimesMs.Count);

			return context.Return(averageMs, worstMs);
		}
		finally
		{
			foreach (var id in instanceIds)
				if (plot.Instances.GetInstance(id) is not null)
					plot.Instances.Remove(id);
		}
	}

	private static ValueTask<int> perf_mod(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var root = ((SceneTree)Engine.GetMainLoop()).Root;
		root.Msaa3D = Viewport.Msaa.Disabled;
		root.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;

		WeakReferenceMessenger.Default.Send(new DisableUiAntialiasingMessage());

		// The world, with its plots and characters, exists only during a session
		if (WorldManager.Instance?.World is { } world)
		{
			if (world.GetNodeOrNull<WorldEnvironment>(SkyPath) is { } sky)
			{
				sky.Set(Sky3DEnabledProperty, false);
				sky.Environment.GlowEnabled = false;
			}

			if (world.GetNodeOrNull<Node3D>(TerrainPath) is { } terrain)
				terrain.Visible = false;

			foreach (var plot in GPlots.GetAll())
				if (GPlotManager.GetHandle(plot.Id).GetNodeOrNull<AreaLight3D>(NightLightPath) is { } nightLight)
					nightLight.Visible = false;

			foreach (var player in GPlayers.GetAll())
				if (GPlayerManager.GetHandle(player.Id).GetNodeOrNull<SpotLight3D>(FlashlightPath) is { } flashlight)
					flashlight.Visible = false;
		}

		Log.Information(
			"Applied performance modifications (may need reapplying after a session starts; setting the time to " +
			"midnight is recommended)");

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_sky_time(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (WorldManager.Instance?.World?.GetNodeOrNull(TimeOfDayPath) is not { } timeOfDay)
		{
			Log.Error("Time of day not found");
			return context.ReturnNothing();
		}

		if (context.IsArgumentOmitted(0))
		{
			timeOfDay.Set(GameTimeEnabledProperty, true);
			timeOfDay.Set(SystemSyncProperty, true);

			Log.Information("Synced lighting time to system clock");
			return context.ReturnNothing();
		}

		var hours = context.GetArgument<float>(0);

		if (!float.IsFinite(hours))
		{
			Log.Error("Invalid lighting time: {Hours} hour(s)", hours);
			return context.ReturnNothing();
		}

		var time = Mathf.PosMod(hours, 24f);

		timeOfDay.Set(GameTimeEnabledProperty, false);
		timeOfDay.Set(SystemSyncProperty, false);
		timeOfDay.Set(CurrentTimeProperty, time);

		Log.Information("Set lighting time to {Hours} hour(s) after midnight", time);

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_static_shader_on(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var isVisible = context.GetArgument<bool>(0);

		if (GMain.GetNodeOrNull<CanvasLayer>(TemporalStaticPath) is not { } temporalStatic)
		{
			Log.Error("{Node} shader not found", TemporalStaticPath);
			return context.ReturnNothing();
		}

		temporalStatic.Visible = isVisible;
		Log.Information("Set {Node} shader visibility to {IsVisible}", TemporalStaticPath, isVisible);

		return context.ReturnNothing();
	}

	private static ValueTask<int> tp_char(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GPlayers.Local is null || GPlayerManager.LocalHandle is not { } handle)
			Log.Error("No local character to teleport");
		else if (context.IsArgumentOmitted(0))
			handle.Respawn();
		else if (context.IsArgumentOmitted(1))
		{
			if (FindPlayer(context.GetArgument<LuaValue>(0)) is not { } target)
				return context.ReturnNothing();

			if (GPlayerManager.GetHandleOrNull(target.Id) is { } other)
				handle.Teleport(other.Body.GlobalPosition + Vector3.Up);
			else
				Log.Error("Player {Name} has no character", target.Name);
		}
		else
		{
			var position = new Vector3(
				context.GetArgument<float>(0), context.GetArgument<float>(1), context.GetArgument<float>(2));

			if (position.IsFinite())
				handle.Teleport(position);
			else
				Log.Error("Invalid teleport position: {Position}", position);
		}

		return context.ReturnNothing();
	}

	private static void AddRandomInstances(GdPlot plot, int count, List<int>? addedIds = null)
	{
		var assetIds = GAssets.GetAll().Select(static asset => asset.Id).ToArray();

		if (assetIds.Length is 0)
			throw new InvalidOperationException("No assets loaded.");

		var instances = plot.Instances;
		var positionRange = (Vector3I)(GPlotManager.GetHandle(plot.Id).GridBoundarySize / 2);
		var random = Random.Shared;

		for (var i = 0; i < count; i++)
		{
			var assetId = assetIds[random.Next(assetIds.Length)];

			Vector3 position;
			Vector3 axis;

			do
			{
				// Retry if floating-point precision produces a vector that is not recognized as normalized
				position = new Vector3(
					random.Next(-positionRange.X, positionRange.X),
					random.Next(0, positionRange.Y * 2) + 1,
					random.Next(-positionRange.Z, positionRange.Z));

				axis = position.Normalized();
			} while (!axis.IsNormalized());

			var rotation = new Quaternion(axis, (float)((random.NextDouble() - 0.5) * Math.Tau));
			var instance = instances.Add(assetId, position, rotation);
			addedIds?.Add(instance.Id);
		}
	}

	// Core enforces no caps, so these functions must
	private static bool IsValidInstanceCount(GdPlot plot, int count)
	{
		var instances = plot.Instances;

		if (count >= 0 && (instances.MaxCount is Unlimited || count <= instances.MaxCount - instances.Count))
			return true;

		Log.Error(
			"Invalid instance count {Count} for plot {PlotId} ({Current}/{Max})",
			count, plot.Id, instances.Count, instances.MaxCount);

		return false;
	}
}
