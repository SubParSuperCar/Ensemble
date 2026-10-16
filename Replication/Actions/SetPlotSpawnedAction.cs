using System.Runtime.CompilerServices;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

public readonly record struct SetPlotSpawnedAction(bool IsSpawned) : INetworkAction<SetPlotSpawnedAction>
{
	public static int TokenCost { get; } = 25;

	public static SetPlotSpawnedAction FromPayload(Array<Variant> payload) => new(payload[0].AsBool());

	public Array<Variant> ToPayload() => [IsSpawned];

	public ActionValidation Validate(ActionSource source)
	{
		if (source.Plot is not { } plot)
			return Reject("Player not an occupant.");

		if (plot.IsSpawned == IsSpawned)
			return Reject(IsSpawned ? "Plot already spawned." : "Plot not spawned.");

		var isOwner = string.Equals(plot.Occupants.Owner?.Player.Id, source.PlayerId, StringComparison.Ordinal);

		// The host has authority over every plot
		return isOwner || source.IsHost ? Accept : Reject("Player not the owner.");
	}

	public void Apply(ActionSource source)
	{
		var plot = source.Plot!;

		if (IsSpawned)
			plot.Spawn();
		else
			plot.Despawn();
	}

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<SetPlotSpawnedAction>();
}
