using Godot;
using Godot.Collections;
using Root.Core.Api.Asset;
using Variant = Godot.Variant;

namespace Root.Core.Gd.Util;

// Helper methods and extensions for converting to/from similar Godot/.NET objects
public static class Converter
{
	public static Vector3 ToGodot(this System.Numerics.Vector3 vector) => new(vector.X, vector.Y, vector.Z);
	public static System.Numerics.Vector3 FromGodot(this Vector3 vector) => new(vector.X, vector.Y, vector.Z);

	public static Quaternion ToGodot(this System.Numerics.Quaternion quaternion) =>
		new(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

	public static System.Numerics.Quaternion FromGodot(this Quaternion quaternion) =>
		new(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

	public static Variant ToGodot(this Api.Asset.Variant variant) =>
		variant.Type switch
		{
			VariantType.Bool => Variant.CreateFrom((bool)variant),
			VariantType.NumInt => Variant.CreateFrom((long)variant),
			VariantType.NumDouble => Variant.CreateFrom((double)variant),
			VariantType.Str => Variant.CreateFrom((string)variant),
			_ => default
		};

	public static Api.Asset.Variant FromGodot(this Variant variant) =>
		variant.VariantType switch
		{
			Variant.Type.Bool => new Api.Asset.Variant(variant.AsBool()),
			Variant.Type.Int => new Api.Asset.Variant(variant.AsInt64()),
			Variant.Type.Float => new Api.Asset.Variant(variant.AsDouble()),
			Variant.Type.String => new Api.Asset.Variant(variant.AsString()),
			_ => Api.Asset.Variant.Null
		};

	public static Dictionary ToGodotProperties(IReadOnlyDictionary<string, Api.Asset.Variant> properties)
	{
		var result = new Dictionary();

		foreach (var (key, value) in properties)
			result.Add(key, value.ToGodot());

		return result;
	}

	public static IReadOnlyDictionary<string, Api.Asset.Variant> FromGodotProperties(Dictionary properties)
	{
		var result = new System.Collections.Generic.Dictionary<string, Api.Asset.Variant>(
			StringComparer.OrdinalIgnoreCase);

		foreach (var (key, value) in properties)
			result[key.AsString()] = value.FromGodot();

		return result;
	}
}
