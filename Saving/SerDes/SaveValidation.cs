using System.Numerics;
using System.Text;
using EnsembleCoreRoot.Api.Assets;

namespace EnsembleRoot.Saving.SerDes;

/// <summary>The limits and rules every save format shares, so a save loads the same whatever its format.</summary>
internal static class SaveValidation
{
	public const int MaxInstanceCount = 1 << 20;
	public const int MaxPropertyCount = ushort.MaxValue - 2;
	public const int MaxTotalPropertyCount = 1 << 20;
	public const int MaxKeyByteCount = 256;
	public const int MaxStringByteCount = 64 * 1024;

	// Like Godot's Quaternion.IsNormalized(), which validates placements
	private const float NormalizedTolerance = 1 / 1024f;

	/// <summary>Normalizes <paramref name="rotation" /> if needed; false if it's zero or overflows.</summary>
	public static bool TryNormalize(Quaternion rotation, out Quaternion normalized)
	{
		var lengthSquared = rotation.LengthSquared();
		var isValid = float.IsFinite(lengthSquared) && lengthSquared is not 0f;

		normalized = isValid && MathF.Abs(lengthSquared - 1f) >= NormalizedTolerance
			? Quaternion.Normalize(rotation)
			: rotation;

		return isValid;
	}

	private static bool IsValid(CoreVariant value) =>
		value.Type switch
		{
			CoreVariantType.Double => double.IsFinite((double)value),
			CoreVariantType.String => Encoding.UTF8.GetByteCount((string)value) <= MaxStringByteCount,
			_ => true
		};

	/// <summary>Checks a save from a deserializer that can't enforce the limits while reading, e.g., JSON.</summary>
	public static void Validate(CreationSaveData data)
	{
		// STJ doesn't enforce nullable annotations, and an omitted rotation reads as zero
		if (data.Instances is not { Count: <= MaxInstanceCount } instances)
			throw new InvalidDataException("Invalid instance count.");

		var totalPropertyCount = 0;

		foreach (var instance in instances)
		{
			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			if (instance is null || !TryNormalize(instance.Rotation, out _))
				throw new InvalidDataException("Invalid instance.");

			if (instance.Properties is not { } properties)
				continue;

			totalPropertyCount += properties.Count;

			if (properties.Count > MaxPropertyCount || totalPropertyCount > MaxTotalPropertyCount)
				throw new InvalidDataException("Invalid property count.");

			var keys = new HashSet<string>(properties.Count, StringComparer.OrdinalIgnoreCase);

			foreach (var (key, value) in properties)
				if (Encoding.UTF8.GetByteCount(key) > MaxKeyByteCount || !keys.Add(key) || !IsValid(value))
					throw new InvalidDataException($"Invalid property: {key}.");
		}
	}
}
