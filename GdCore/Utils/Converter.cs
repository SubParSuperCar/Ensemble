using EnsembleCoreRoot.Api.Assets;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.GdCore.Utils;

public static class Converter
{
	public static Vector3 ToGodot(this System.Numerics.Vector3 vector) => new(vector.X, vector.Y, vector.Z);
	public static System.Numerics.Vector3 FromGodot(this Vector3 vector) => new(vector.X, vector.Y, vector.Z);

	public static Quaternion ToGodot(this System.Numerics.Quaternion quaternion) =>
		new(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

	public static System.Numerics.Quaternion FromGodot(this Quaternion quaternion) =>
		new(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

	public static Variant ToGodot(this CoreVariant variant) =>
		variant.Type switch
		{
			CoreVariantType.Boolean => Variant.CreateFrom((bool)variant),
			CoreVariantType.Int64 => Variant.CreateFrom((long)variant),
			CoreVariantType.Double => Variant.CreateFrom((double)variant),
			CoreVariantType.String => Variant.CreateFrom((string)variant),
			_ => default
		};

	extension(Variant variant)
	{
		public CoreVariant FromGodot() =>
			variant.VariantType switch
			{
				Variant.Type.Bool => new CoreVariant(variant.AsBool()),
				Variant.Type.Int => new CoreVariant(variant.AsInt64()),
				Variant.Type.Float => new CoreVariant(variant.AsDouble()),
				Variant.Type.String or Variant.Type.StringName => new CoreVariant(variant.AsString()),
				_ => CoreVariant.Null
			};

		/// <summary>
		///     Whether <see cref="FromGodot(Variant)" /> keeps the value: nil or a type <see cref="CoreVariant" />
		///     holds, where any other converts to Null.
		/// </summary>
		public bool IsConvertible() => variant.VariantType is Variant.Type.Nil || !variant.FromGodot().IsNull;
	}

	public static Dictionary ToGodotProperties(IReadOnlyDictionary<string, CoreVariant> properties)
	{
		var result = new Dictionary();

		foreach (var (key, value) in properties)
			result.Add(key, value.ToGodot());

		return result;
	}

	public static IReadOnlyDictionary<string, CoreVariant> FromGodotProperties(Dictionary properties)
	{
		var result = new System.Collections.Generic.Dictionary<string, CoreVariant>(
			properties.Count, StringComparer.OrdinalIgnoreCase);

		foreach (var (key, value) in properties)
			result[key.AsString()] = value.FromGodot();

		return result;
	}
}
