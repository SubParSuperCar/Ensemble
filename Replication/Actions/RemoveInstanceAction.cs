using System.Runtime.CompilerServices;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

public readonly record struct RemoveInstanceAction(InstanceReference Instance) : INetworkAction<RemoveInstanceAction>
{
	public static RemoveInstanceAction FromPayload(Array<Variant> payload) =>
		new(InstanceReference.FromPayload(payload));

	public Array<Variant> ToPayload() => Instance.ToPayload();

	public ActionValidation Validate(ActionSource source)
	{
		if (source.Plot is not { IsSpawned: false } plot)
			return Reject("Plot not editable.");

		return Instance.Resolve(plot) is null ? Reject("Instance not found.") : Accept;
	}

	public void Apply(ActionSource source) => source.Plot!.Instances.Remove(Instance.Id);

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<RemoveInstanceAction>();
}
