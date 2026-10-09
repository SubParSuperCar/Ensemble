using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.GdCore.Utils;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

/// <remarks>Only existing properties can be set, and only to values of the same type.</remarks>
public readonly record struct SetPropertiesAction(InstanceReference Instance, Dictionary Properties)
	: INetworkAction<SetPropertiesAction>
{
	private const int MaxStringLength = 256;

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

		var current = instance.Properties.Source.All;

		foreach (var (key, value) in Properties)
		{
			var name = key.VariantType is Variant.Type.String or Variant.Type.StringName ? key.AsString() : null;

			if (name is null || !current.TryGetValue(name, out var existing))
				return Reject("Property not found.");

			var updated = value.FromGodot();

			if (updated.Type != existing.Type)
				return Reject($"Property {name} must stay {existing.Type}.");

			// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
			switch (updated.Type)
			{
				case CoreVariantType.Double when !double.IsFinite((double)updated):
					return Reject($"Property {name} not finite.");

				case CoreVariantType.String when ((string)updated).Length > MaxStringLength:
					return Reject($"Property {name} too long.");
			}
		}

		return Accept;
	}

	public void Apply(ActionSource source) => Instance.Resolve(source.Plot!)!.Properties.UpdateAll(Properties);

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<SetPropertiesAction>();
}
