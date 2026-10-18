using System.Runtime.CompilerServices;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

public readonly record struct SetPlotSpawnedAction(bool IsSpawned) : INetworkAction<SetPlotSpawnedAction>
{
	// Spawning builds the creation on every peer, so it costs more the bigger the plot; 2000 instances cost 87
	private const int InstancesPerToken = 32;

	public static int TokenCost { get; } = 25;

	public static int GetTokenCost(Array<Variant> payload, ActionSource source) =>
		payload is [{ VariantType: Variant.Type.Bool } isSpawned] && isSpawned.AsBool() && source.Plot is { } plot
			? TokenCost + plot.Instances.Count / InstancesPerToken
			: TokenCost;

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
