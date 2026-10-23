using System.Runtime.CompilerServices;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

/// <remarks>Only existing properties can be set, and only to values of the same type.</remarks>
public readonly record struct SetPropertiesAction(InstanceReference Instance, Dictionary Properties)
	: INetworkAction<SetPropertiesAction>
{
	public static int TokenCost { get; } = 5;

	public static SetPropertiesAction FromPayload(Array<Variant> payload) =>
		new(InstanceReference.FromPayload(payload[0].AsGodotArray<Variant>()), payload[1].AsGodotDictionary());

	public Array<Variant> ToPayload() => [Instance.ToPayload(), Properties];

	public ActionValidation Validate(ActionSource source)
	{
		if (source.Plot is not { IsSpawned: false } plot)
			return Reject("Plot not editable.");

		if (Instance.Resolve(plot) is not { } instance)
			return Reject("Instance not found.");

		if (Properties.Count is 0)
			return Reject("No properties.");

		return PropertyRules.Validate(instance.Properties.Source.All, Properties);
	}

	public void Apply(ActionSource source) => Instance.Resolve(source.Plot!)!.Properties.UpdateAll(Properties);

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<SetPropertiesAction>();
}
