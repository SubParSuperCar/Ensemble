namespace EnsembleCoreRoot.Api.Assets;

/// <summary>
///     The rules every property value follows, which <see cref="IProperties" /> enforces: it keeps its property's type,
///     a double stays finite, and a string stays within <see cref="MaxStringLength" />.
/// </summary>
public static class PropertyRules
{
	public const int MaxStringLength = 256;

	/// <summary>Gets why <paramref name="value" /> can't replace <paramref name="current" />, else null.</summary>
	public static string? Check(string key, CoreVariant value, CoreVariant current)
	{
		if (value.Type != current.Type)
			return $"Property {key} must stay {current.Type}.";

		return value.Type switch
		{
			CoreVariantType.Double when !double.IsFinite((double)value) => $"Property {key} not finite.",
			CoreVariantType.String when ((string)value).Length > MaxStringLength => $"Property {key} too long.",
			_ => null
		};
	}
}
