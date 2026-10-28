using System.Globalization;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Sessions.Actions;
using Lua;
using Serilog;

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	private static ValueTask<int> dmp_creation(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (FindPlot(context.GetArgumentOrNull<int>(0), nameof(dmp_creation)) is not { } plot)
			return context.ReturnNothing();

		var handle = GPlotManager.GetHandle(plot.Id);

		if (handle.Creation is not { } creation)
			Log.Information("Plot {PlotId}'s creation isn't spawned", plot.Id);
		else
			Log.Information(
				"Plot {PlotId}'s creation: {Count} assemblies, simulated by peer {PeerId} (epoch {Epoch}, " +
				"arbitrated {IsArbitrated})\n{Assemblies}",
				plot.Id, creation.Assemblies.Count, handle.SimulatorPeerId, handle.SimulationEpoch,
				handle.ArbitratedSimulatorPeerId is not null, string.Join('\n', creation.Assemblies));

		return context.ReturnNothing();
	}

	private static ValueTask<int> kick_occupant(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (FindPlayer(context.GetArgument<LuaValue>(0)) is { } player)
			new KickOccupantAction(player.Id).Submit();

		return context.ReturnNothing();
	}

	private static ValueTask<int> dmp_plots(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var plots = GPlots.GetAll().OrderBy(static plot => plot.Id).Select(static plot =>
		{
			var (instances, occupants) = (plot.Instances, plot.Occupants);
			var names = string.Join(", ", occupants.GetAll().Select(static occupant => occupant.Player.Name));

			return string.Create(
				CultureInfo.InvariantCulture,
				$"Plot {plot.Id}: {instances.Count}/{FormatMax(instances.MaxCount)} instance(s), " +
				$"{occupants.Count}/{FormatMax(occupants.MaxCount)} occupant(s) " +
				$"(Owner={occupants.Owner?.Player.Name}, Occupants=[{names}], IsSpawned={plot.IsSpawned})");
		});

		Log.Information("Plots (local {LocalPlotId}):\n{Plots}", LocalPlot?.Id, string.Join('\n', plots));

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_plot(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var plotId = context.GetArgumentOrNull<int>(0);

		if (plotId is { } id && GPlots.GetPlot(id) is null)
			Log.Error("Plot with ID {PlotId} not found", id);
		else
			new SetPlotAction(plotId).Submit();

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_plot_simulator(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var isReset = context.IsArgumentOmitted(1);
		var peerId = isReset ? null : FindPeerId(context.GetArgument<LuaValue>(1));

		if (
			FindPlot(context.GetArgument<int>(0), nameof(set_plot_simulator)) is not { } plot ||
			(!isReset && peerId is null))
			return context.ReturnNothing();

		if (GPlotManager.GetHandle(plot.Id).SetSimulatorOverride(peerId))
			Log.Information("Set plot {PlotId}'s simulator override to {PeerId}", plot.Id, peerId);
		else
			Log.Error("Only the host can set simulators, and only to registered peers");

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_plot_spawned(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (LocalPlot is null)
			Log.Error("{Function} needs a claimed plot", nameof(set_plot_spawned));
		else
			new SetPlotSpawnedAction(context.GetArgument<bool>(0)).Submit();

		return context.ReturnNothing();
	}

	private static string FormatMax(int maxCount) =>
		maxCount is Unlimited ? "unlimited" : maxCount.ToString(CultureInfo.InvariantCulture);
}
