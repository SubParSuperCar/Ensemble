using Godot;
using Godot.Collections;
using Root.Core.Api.Asset;
using Variant = Godot.Variant;

namespace Root.Core.Gd.Util;

public static class GdConvert
{
	public static Vector3 ToGodot(this System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);
	public static System.Numerics.Vector3 FromGodot(this Vector3 v) => new(v.X, v.Y, v.Z);

	public static Quaternion ToGodot(this System.Numerics.Quaternion q) => new(q.X, q.Y, q.Z, q.W);
	public static System.Numerics.Quaternion FromGodot(this Quaternion q) => new(q.X, q.Y, q.Z, q.W);

	public static Variant ToGodot(this Api.Asset.Variant v) => v.Type switch
	{
		VariantType.Bool => Variant.CreateFrom((bool)v),
		VariantType.NumInt => Variant.CreateFrom((long)v),
		VariantType.NumDouble => Variant.CreateFrom((double)v),
		VariantType.Str => Variant.CreateFrom((string)v),
		_ => default
	};

	public static Api.Asset.Variant FromGodot(this Variant v) => v.VariantType switch
	{
		Variant.Type.Bool => new Api.Asset.Variant(v.AsBool()),
		Variant.Type.Int => new Api.Asset.Variant(v.AsInt64()),
		Variant.Type.Float => new Api.Asset.Variant(v.AsDouble()),
		Variant.Type.String => new Api.Asset.Variant(v.AsString()),
		_ => Api.Asset.Variant.Null
	};

	public static Dictionary ToGodotProperties(IReadOnlyDictionary<string, Api.Asset.Variant> properties)
	{
		var converted = new Dictionary();

		foreach (var (key, value) in properties)
			converted.Add(key, value.ToGodot());

		return converted;
	}

	public static IReadOnlyDictionary<string, Api.Asset.Variant> FromGodotProperties(Dictionary properties)
	{
		var converted = new System.Collections.Generic.Dictionary<string, Api.Asset.Variant>(
			StringComparer.OrdinalIgnoreCase);

		foreach (var (key, value) in properties)
			converted[key.AsString()] = value.FromGodot();

		return converted;
	}
}
