using System.Runtime.CompilerServices;
using EnsembleRoot.SessionManager.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.SessionManager.Actions.ActionValidation;

namespace EnsembleRoot.Actions;

public readonly record struct SetPlotAction(int? PlotId) : INetworkAction<SetPlotAction>
{
	public static SetPlotAction FromPayload(Array<Variant> payload) =>
		new(payload[0].AsInt32() is var plotId and not None ? plotId : null);

	public Array<Variant> ToPayload() => [PlotId ?? None];

	public ActionValidation Validate(ActionSource source)
	{
		if (source.Occupant is not { } occupant)
			return Reject("Player not an occupant.");

		if (PlotId is not { } plotId || occupant.Plot?.Id == plotId)
			return Accept;

		if (GPlots.GetPlot(plotId)?.Occupants is not { } occupants)
			return Reject("Plot not found.");

		return occupants.MaxCount is not Unlimited && occupants.Count >= occupants.MaxCount
			? Reject("Plot full.")
			: Accept;
	}

	public void Apply(ActionSource source) => GPlots.SetPlot(source.PlayerId, PlotId ?? None);

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<SetPlotAction>();
}
