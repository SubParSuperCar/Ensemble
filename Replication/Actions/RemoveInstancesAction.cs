using System.Runtime.CompilerServices;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

public readonly record struct RemoveInstancesAction(IReadOnlyList<InstanceReference> Instances)
	: INetworkAction<RemoveInstancesAction>
{
	public const int MaxCount = 256;

	// A full batch costs a sixth of a peer's token bucket, so one marquee can delete a few thousand instances
	private const int CountPerToken = 16;

	public static int GetTokenCost(Array<Variant> payload, ActionSource source) =>
		(payload.Count + CountPerToken - 1) / CountPerToken;

	public static RemoveInstancesAction FromPayload(Array<Variant> payload) =>
		new([.. payload.Select(static reference => InstanceReference.FromPayload(reference.AsGodotArray<Variant>()))]);

	public Array<Variant> ToPayload() => [.. Instances.Select(static reference => (Variant)reference.ToPayload())];

	public ActionValidation Validate(ActionSource source)
	{
		if (source.Plot is not { IsSpawned: false } plot)
			return Reject("Plot not editable.");

		if (Instances.Count is 0 or > MaxCount)
			return Reject("Instance count invalid.");

		// Others may remove some first; the rest still go
		return Instances.Any(reference => reference.Resolve(plot) is not null)
			? Accept
			: Reject("Instances not found.");
	}

	public void Apply(ActionSource source)
	{
		var plot = source.Plot!;

		// Resolving each one also skips duplicates
		foreach (var reference in Instances)
			if (reference.Resolve(plot) is not null)
				plot.Instances.Remove(reference.Id);
	}

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<RemoveInstancesAction>();
}
