using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.GdCore.Utils;
using EnsembleRoot.SessionManager.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.SessionManager.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

/// <remarks>Only existing properties can be set, and only to values of the same type.</remarks>
public readonly record struct SetPropertiesAction(InstanceReference Instance, Dictionary Properties)
	: INetworkAction<SetPropertiesAction>
{
	private const int MaxStringLength = 256;

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
			if (key.VariantType is not (Variant.Type.String or Variant.Type.StringName) ||
				!current.TryGetValue(key.AsString(), out var existing))
				return Reject("Property not found.");

			var updated = value.FromGodot();

			if (updated.Type != existing.Type)
				return Reject($"Property {key.AsString()} must stay {existing.Type}.");

			// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
			switch (updated.Type)
			{
				case CoreVariantType.Double when !double.IsFinite((double)updated):
					return Reject($"Property {key.AsString()} not finite.");

				case CoreVariantType.String when ((string)updated).Length > MaxStringLength:
					return Reject($"Property {key.AsString()} too long.");
			}
		}

		return Accept;
	}

	public void Apply(ActionSource source) => Instance.Resolve(source.Plot!)!.Properties.UpdateAll(Properties);

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<SetPropertiesAction>();
}
