using CoreRoot.Api.Assets;
using Godot;
using Godot.Collections;
using Variant = Godot.Variant;

namespace Root.GdCore.Utils;

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
			CoreVariantType.Bool => Variant.CreateFrom((bool)variant),
			CoreVariantType.NumInt => Variant.CreateFrom((long)variant),
			CoreVariantType.NumDouble => Variant.CreateFrom((double)variant),
			CoreVariantType.Str => Variant.CreateFrom((string)variant),
			_ => default
		};

	public static CoreVariant FromGodot(this Variant variant) =>
		variant.VariantType switch
		{
			Variant.Type.Bool => new CoreVariant(variant.AsBool()),
			Variant.Type.Int => new CoreVariant(variant.AsInt64()),
			Variant.Type.Float => new CoreVariant(variant.AsDouble()),
			Variant.Type.String => new CoreVariant(variant.AsString()),
			_ => CoreVariant.Null
		};

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
			StringComparer.OrdinalIgnoreCase);

		foreach (var (key, value) in properties)
			result[key.AsString()] = value.FromGodot();

		return result;
	}
}
