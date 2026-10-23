using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.GdCore.Utils;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

/// <summary>The rules property edits follow: only existing properties, each keeping its type, within limits.</summary>
internal static class PropertyRules
{
	private const int MaxStringLength = 256;

	public static ActionValidation Validate(IReadOnlyDictionary<string, CoreVariant> current, Dictionary properties)
	{
		foreach (var (key, value) in properties)
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
}
