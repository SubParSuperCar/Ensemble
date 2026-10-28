using System.Runtime.CompilerServices;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

public readonly record struct ClearInstancesAction : INetworkAction<ClearInstancesAction>
{
	public static int TokenCost { get; } = 25;

	public static ClearInstancesAction FromPayload(Array<Variant> payload) => new();

	public Array<Variant> ToPayload() => [];

	public ActionValidation Validate(ActionSource source)
	{
		if (source.Plot is not { IsSpawned: false } plot)
			return Reject("Plot not editable.");

		// The host has authority over every plot
		return source.IsHost || source.Owns(plot) ? Accept : Reject("Player not the owner.");
	}

	public void Apply(ActionSource source) => source.Plot!.Instances.Clear();

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<ClearInstancesAction>();
}
