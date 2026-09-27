using System.Runtime.CompilerServices;
using EnsembleRoot.SessionManager.Actions;
using static EnsembleRoot.SessionManager.Actions.ActionValidation;
using GArray = Godot.Collections.Array;

namespace EnsembleRoot.Actions;

public readonly record struct SetPlotAction(int? PlotId) : INetworkAction<SetPlotAction>
{
	public static SetPlotAction FromPayload(GArray payload) =>
		new(payload[0].AsInt32() is var plotId and not None ? plotId : null);

	public GArray ToPayload() => [PlotId ?? None];

	public ActionValidation Validate(ActionSource source)
	{
		if (GPlots.GetOccupant(source.PlayerId) is not { } occupant)
			return Reject("Player is not an occupant.");

		if (PlotId is not { } plotId || occupant.Plot?.Id == plotId)
			return Accept;

		if (GPlots.GetPlot(plotId)?.Occupants is not { } occupants)
			return Reject("Plot not found.");

		return occupants.MaxCount is not Unlimited && occupants.Count >= occupants.MaxCount
			? Reject("Plot is full.")
			: Accept;
	}

	public void Apply(ActionSource source) => GPlots.SetPlot(source.PlayerId, PlotId ?? None);

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<SetPlotAction>();
}
