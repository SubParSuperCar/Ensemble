using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.GdCore.Utils;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

/// <summary>The rules property edits follow: only existing properties, each keeping its type, within limits.</summary>
internal static class PropertyRules
{
	public static ActionValidation Validate(IReadOnlyDictionary<string, CoreVariant> current, Dictionary properties)
	{
		foreach (var (key, value) in properties)
		{
			var name = key.VariantType is Variant.Type.String or Variant.Type.StringName ? key.AsString() : null;

			if (name is null || !current.TryGetValue(name, out var existing))
				return Reject("Property not found.");

			// Unsupported Godot types convert to Null, which would pass for a Null property here but throw when applied
			if (!GdProperties.IsSameType(value, existing))
				return Reject($"Property {name} must stay {existing.Type}.");

			if (Validate(name, existing, value.FromGodot()) is { IsValid: false } validation)
				return validation;
		}

		return Accept;
	}

	public static ActionValidation Validate(string name, CoreVariant existing, CoreVariant updated)
	{
		if (updated.Type != existing.Type)
			return Reject($"Property {name} must stay {existing.Type}.");

		if (updated.Type is CoreVariantType.Double && !double.IsFinite((double)updated))
			return Reject($"Property {name} not finite.");

		return updated.Type is CoreVariantType.String && ((string)updated).Length > MaxPropertyStringLength
			? Reject($"Property {name} too long.")
			: Accept;
	}
}
