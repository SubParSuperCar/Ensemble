using System.Diagnostics;
using EnsembleRoot.Scripts.World;
using EnsembleRoot.SessionManager.Api;
using Godot;
using Lua;
using Serilog;

// ReSharper disable InconsistentNaming

namespace EnsembleRoot.Common.Execution;

public static partial class LuaExecutor
{
	private static ValueTask<int> add_rand_insts(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		if (!IsSinglePlayer(nameof(add_rand_insts)))
			return context.ReturnNothing();

		var plotId = context.GetArgument<int>(0);
		var count = context.GetArgument<int>(1);
		var positionRange = GPlotManager.GetHandle(plotId).GridBoundarySize / 2;

		var instances = GPlots.GetPlot(plotId)!.Instances;
		var assetIds = GAssets.GetAll().Select(static asset => asset.Id).ToArray();
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
			count,
			plotId,
			stopwatch.Elapsed.TotalMilliseconds);

		return context.ReturnNothing();
	}

	private static ValueTask<int> clr_insts(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		if (!IsSinglePlayer(nameof(clr_insts)))
			return context.ReturnNothing();

		var plotId = context.GetArgument<int>(0);
		var instances = GPlots.GetPlot(plotId)!.Instances;
		var count = instances.Count;

		var stopwatch = Stopwatch.StartNew();
		instances.Clear();

		stopwatch.Stop();
		Log.Information(
			"Removed {Count} instance(s) from plot with id {PlotId} in {ElapsedMs:F3} ms (deferred)",
			count,
			plotId,
			stopwatch.Elapsed.TotalMilliseconds);

		return context.ReturnNothing();
	}

	private static bool IsSinglePlayer(string function)
	{
		if (GSessionManager.Mode is SessionMode.SinglePlayer)
			return true;

		Log.Error("{Function} edits plots directly, so it is single-player only", function);
		return false;
	}

	private static ValueTask<int> perf_mod(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		if ((Engine.GetMainLoop() as SceneTree)?.Root is { } root)
		{
			root.Msaa3D = Viewport.Msaa.Disabled;
			root.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
		}

		if (WorldManager.Instance?.World is { } world)
		{
			if (world.GetNodeOrNull<WorldEnvironment>("Sky") is { } sky)
				sky.Set("sky3d_enabled", false);

			if (world.GetNodeOrNull<Node3D>("Terrain") is { } terrain)
				terrain.Visible = false;
		}

		foreach (var plot in GPlotManager.Handles.Values)
			if (plot.GetNodeOrNull<AreaLight3D>("Night Light") is { } nightLight)
				nightLight.Visible = false;

		foreach (var player in GPlayerManager.Handles.Values)
			if (player.GetNodeOrNull<SpotLight3D>("Character/Flashlight") is { } flashlight)
				flashlight.Visible = false;

		Log.Information(
			"Performance modification applied. It may need to be reapplied upon session startups. " +
			"Setting time to midnight is recommended");

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_static_shader_on(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var isVisible = context.GetArgument<bool>(0);
		Log.Information("Setting Temporal Static shader visibility to: {IsVisible}", isVisible);

		GMain.GetNodeOrNull<CanvasLayer>("Temporal Static")?.Visible = isVisible;

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_time(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		SetTimeOfDay(context);

		return context.ReturnNothing();
	}

	private static void SetTimeOfDay(LuaFunctionExecutionContext context)
	{
		var timeOfDay = WorldManager.Instance?.World?.GetNodeOrNull("Sky/TimeOfDay");
		if (timeOfDay is null)
			return;

		if (!context.HasArgument(0))
		{
			timeOfDay.Set("game_time_enabled", true);
			timeOfDay.Set("system_sync", true);

			Log.Information("Synced lighting time to system clock");
			return;
		}

		var time = context.GetArgument<float>(0) % 24;
		timeOfDay.Set("game_time_enabled", false);
		timeOfDay.Set("system_sync", false);
		timeOfDay.Set("current_time", time);

		Log.Information("Set lighting time to {Hours} hour(s) after midnight", time);
	}

	private static ValueTask<int> tp_char(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
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
				Log.Error("Player not found: {Query}", query);
		}
		else
			handle.Teleport(new Vector3(
				context.GetArgument<float>(0),
				context.GetArgument<float>(1),
				context.GetArgument<float>(2)));

		return context.ReturnNothing();
	}
}
