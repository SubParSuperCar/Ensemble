using System.Runtime.CompilerServices;
using EnsembleRoot.SessionManager.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.SessionManager.Actions.ActionValidation;

namespace EnsembleRoot.Networking.Actions;

public readonly record struct RemoveInstanceAction(int InstanceId) : INetworkAction<RemoveInstanceAction>
{
	public static RemoveInstanceAction FromPayload(Array<Variant> payload) => new(payload[0].AsInt32());

	public Array<Variant> ToPayload() => [InstanceId];

	public ActionValidation Validate(ActionSource source)
	{
		if (source.Plot is not { IsSpawned: false } plot)
			return Reject("Plot not editable.");

		return plot.Instances.GetInstance(InstanceId) is null ? Reject("Instance not found.") : Accept;
	}

	public void Apply(ActionSource source) => source.Plot!.Instances.Remove(InstanceId);

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<RemoveInstanceAction>();
}
