using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Sessions.Actions;
using Lua;
using Serilog;

// ReSharper disable InconsistentNaming

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	private static ValueTask<int> dmp_creation(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var plotId = context.IsArgumentOmitted(0) ? LocalPlot?.Id : context.GetArgument<int>(0);

		if (plotId is not { } id || GPlotManager.GetHandleOrNull(id) is not { } handle)
		{
			Log.Error("{Function} needs a plot ID or a claimed plot", nameof(dmp_creation));
			return context.ReturnNothing();
		}

		if (handle.Assemblies.Count is 0)
			Log.Information("Plot {PlotId}'s creation isn't spawned", id);
		else
			Log.Information(
				"Plot {PlotId}'s creation: {Count} assemblies, simulated by peer {PeerId} (epoch {Epoch})\n{Assemblies}",
				id, handle.Assemblies.Count, handle.SimulatorPeerId, handle.SimulationEpoch,
				string.Join('\n', handle.Assemblies));

		return context.ReturnNothing();
	}

	private static ValueTask<int> kick_occupant(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var query = context.GetArgument<string>(0);

		if (FindPlayer(query) is { } player)
			new KickOccupantAction(player.Id).Submit();
		else
			Log.Error("Player with id or name {Query} not found", query);

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_plot_simulator(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var plotId = context.GetArgument<int>(0);
		int? peerId = context.IsArgumentOmitted(1) ? null : context.GetArgument<int>(1);

		if (GPlotManager.GetHandleOrNull(plotId) is not { } handle)
			Log.Error("Plot with id {PlotId} not found", plotId);
		else if (!handle.SetSimulatorOverride(peerId))
			Log.Error("Only the host can set simulators, and only to registered peers");
		else
			Log.Information("Set plot {PlotId}'s simulator override to {PeerId}", plotId, peerId);

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_plot_spawned(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GetLocalPlot(nameof(set_plot_spawned)) is not null)
			new SetPlotSpawnedAction(context.GetArgument<bool>(0)).Submit();

		return context.ReturnNothing();
	}
}
