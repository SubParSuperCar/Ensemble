using System.Diagnostics;
using EnsembleRoot.Scripts.World;
using EnsembleRoot.Sessions.Api;
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

	private static readonly StringName Sky3DEnabledProperty = "sky3d_enabled";
	private static readonly StringName GameTimeEnabledProperty = "game_time_enabled";
	private static readonly StringName SystemSyncProperty = "system_sync";
	private static readonly StringName CurrentTimeProperty = "current_time";

	private static ValueTask<int> add_rand_insts(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (!IsSinglePlayer(nameof(add_rand_insts)))
			return context.ReturnNothing();

		var plotId = context.GetArgument<int>(0);
		var count = context.GetArgument<int>(1);

		if (GPlots.GetPlot(plotId) is not { } plot)
		{
			Log.Error("Plot with id {PlotId} not found", plotId);
			return context.ReturnNothing();
		}

		var assetIds = GAssets.GetAll().Select(static asset => asset.Id).ToArray();

		if (assetIds.Length is 0)
		{
			Log.Error("No assets loaded");
			return context.ReturnNothing();
		}

		var instances = plot.Instances;
		var positionRange = GPlotManager.GetHandle(plotId).GridBoundarySize / 2;
		var random = Random.Shared;

		var stopwatch = Stopwatch.StartNew();
		for (var i = 0; i < count; i++)
		{
			var assetId = assetIds[random.Next(assetIds.Length)];

			Vector3 position;
			Vector3 axis;

			do
			{
				// Retry if floating-point precision produces a vector that is not recognized as normalized
				position = new Vector3(
					random.Next(-(int)positionRange.X, (int)positionRange.X),
					random.Next(0, (int)positionRange.Y * 2) + 1,
					random.Next(-(int)positionRange.Z, (int)positionRange.Z));

				axis = position.Normalized();
			} while (!axis.IsNormalized());

			var rotation = new Quaternion(axis, (float)((random.NextDouble() - 0.5) * Math.Tau));
			instances.Add(assetId, position, rotation);
		}

		stopwatch.Stop();
		Log.Information(
			"Added {Count} instance(s) to plot with id {PlotId} in {ElapsedMs:F3} ms",
			count, plotId, stopwatch.Elapsed.TotalMilliseconds);

		return context.ReturnNothing();
	}

	private static ValueTask<int> clr_insts(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (!IsSinglePlayer(nameof(clr_insts)))
			return context.ReturnNothing();

		var plotId = context.GetArgument<int>(0);

		if (GPlots.GetPlot(plotId) is not { } plot)
		{
			Log.Error("Plot with id {PlotId} not found", plotId);
			return context.ReturnNothing();
		}

		var instances = plot.Instances;
		var count = instances.Count;

		var stopwatch = Stopwatch.StartNew();
		instances.Clear();

		stopwatch.Stop();
		Log.Information(
			"Removed {Count} instance(s) from plot with id {PlotId} in {ElapsedMs:F3} ms (deferred)",
			count, plotId, stopwatch.Elapsed.TotalMilliseconds);

		return context.ReturnNothing();
	}

	private static ValueTask<int> perf_mod(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if ((Engine.GetMainLoop() as SceneTree)?.Root is { } root)
		{
			root.Msaa3D = Viewport.Msaa.Disabled;
			root.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
		}

		if (WorldManager.Instance?.World is { } world)
		{
			if (world.GetNodeOrNull<WorldEnvironment>(SkyPath) is { } sky)
				sky.Set(Sky3DEnabledProperty, false);

			if (world.GetNodeOrNull<Node3D>(TerrainPath) is { } terrain)
				terrain.Visible = false;
		}

		foreach (var plot in GPlotManager.Handles.Values)
			if (plot.GetNodeOrNull<AreaLight3D>(NightLightPath) is { } nightLight)
				nightLight.Visible = false;

		foreach (var player in GPlayerManager.Handles.Values)
			if (player.GetNodeOrNull<SpotLight3D>(FlashlightPath) is { } flashlight)
				flashlight.Visible = false;

		Log.Information(
			"Applied performance modifications (may need reapplying after a session starts; setting the time to " +
			"midnight is recommended)");

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_static_shader_on(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var isVisible = context.GetArgument<bool>(0);

		if (GMain.GetNodeOrNull<CanvasLayer>(TemporalStaticPath) is not { } temporalStatic)
		{
			Log.Error("Temporal Static shader not found");
			return context.ReturnNothing();
		}

		temporalStatic.Visible = isVisible;
		Log.Information("Set Temporal Static shader visibility to {IsVisible}", isVisible);

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_time(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (WorldManager.Instance?.World?.GetNodeOrNull(TimeOfDayPath) is not { } timeOfDay)
		{
			Log.Error("Time of day not found");
			return context.ReturnNothing();
		}

		if (!context.HasArgument(0))
		{
			timeOfDay.Set(GameTimeEnabledProperty, true);
			timeOfDay.Set(SystemSyncProperty, true);

			Log.Information("Synced lighting time to system clock");
			return context.ReturnNothing();
		}

		var time = Mathf.PosMod(context.GetArgument<float>(0), 24f);

		timeOfDay.Set(GameTimeEnabledProperty, false);
		timeOfDay.Set(SystemSyncProperty, false);
		timeOfDay.Set(CurrentTimeProperty, time);

		Log.Information("Set lighting time to {Hours} hour(s) after midnight", time);
		return context.ReturnNothing();
	}

	private static ValueTask<int> tp_char(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GPlayers.Local is null || GPlayerManager.LocalHandle is not { } handle)
			Log.Error("No local character to teleport");
		else if (!context.HasArgument(0))
			handle.Respawn();
		else if (context.GetArgument<LuaValue>(0) is { Type: LuaValueType.String } argument)
		{
			var query = argument.Read<string>();

			var target = GPlayers.GetAll().FirstOrDefault(player =>
				string.Equals(player.Id, query, StringComparison.Ordinal) ||
				string.Equals(player.Name, query, StringComparison.Ordinal));

			if (target is not null && GPlayerManager.GetHandleOrNull(target.Id) is { } other)
				handle.Teleport(other.Body.GlobalPosition + Vector3.Up);
			else
				Log.Error("Player with id or name {Query} not found", query);
		}
		else
			handle.Teleport(new Vector3(
				context.GetArgument<float>(0), context.GetArgument<float>(1), context.GetArgument<float>(2)));

		return context.ReturnNothing();
	}

	private static bool IsSinglePlayer(string function)
	{
		if (GSessionManager.Mode is SessionMode.SinglePlayer)
			return true;

		Log.Error("{Function} is only available in Single-Player, since it edits plots directly", function);
		return false;
	}
}
