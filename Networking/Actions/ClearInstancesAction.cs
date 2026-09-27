using System.Runtime.CompilerServices;
using EnsembleRoot.SessionManager.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.SessionManager.Actions.ActionValidation;

namespace EnsembleRoot.Networking.Actions;

public readonly record struct ClearInstancesAction : INetworkAction<ClearInstancesAction>
{
	public static ClearInstancesAction FromPayload(Array<Variant> payload) => new();

	public Array<Variant> ToPayload() => [];

	public ActionValidation Validate(ActionSource source)
	{
		if (source.Plot is not { IsSpawned: false } plot)
			return Reject("Plot not editable.");

		return string.Equals(plot.Occupants.Owner?.Player.Id, source.PlayerId, StringComparison.Ordinal)
			? Accept
			: Reject("Player not the owner.");
	}

	public void Apply(ActionSource source) => source.Plot!.Instances.Clear();

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<ClearInstancesAction>();
}
