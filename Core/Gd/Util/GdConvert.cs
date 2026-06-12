using System.Numerics;
using Godot.Collections;
using Root.Core.Api.Asset;

namespace Root.Core.Gd.Util;

public static class GdConvert
{
	public static Vector3 FromGodot(this Godot.Vector3 v) => new(v.X, v.Y, v.Z);
	public static Godot.Vector3 ToGodot(this Vector3 v) => new(v.X, v.Y, v.Z);

	public static Quaternion FromGodot(this Godot.Quaternion q) => new(q.X, q.Y, q.Z, q.W);
	public static Godot.Quaternion ToGodot(this Quaternion q) => new(q.X, q.Y, q.Z, q.W);

	public static Variant FromGodot(this Godot.Variant v) => v.VariantType switch
	{
		Godot.Variant.Type.Bool => new Variant(v.AsBool()),
		Godot.Variant.Type.Int => new Variant(v.AsInt64()),
		Godot.Variant.Type.Float => new Variant(v.AsDouble()),
		Godot.Variant.Type.String => new Variant(v.AsString()),
		_ => Variant.Null
	};

	public static Godot.Variant ToGodot(this Variant v) => v.Type switch
	{
		VariantType.Bool => Godot.Variant.CreateFrom((bool)v),
		VariantType.NumInt => Godot.Variant.CreateFrom((long)v),
		VariantType.NumDouble => Godot.Variant.CreateFrom((double)v),
		VariantType.Str => Godot.Variant.CreateFrom((string)v),
		_ => default
	};

	public static Dictionary ToGodotProperties(IReadOnlyDictionary<string, Variant> properties)
	{
		var gdProperties = new Dictionary();

		foreach (var (key, value) in properties)
			gdProperties.Add(key, value.ToGodot());

		return gdProperties;
	}

	public static IReadOnlyDictionary<string, Variant> FromGodotProperties(Dictionary gdProperties)
	{
		var properties = new System.Collections.Generic.Dictionary<string, Variant>(StringComparer.OrdinalIgnoreCase);

		foreach (var key in gdProperties.Keys)
			properties[key.AsString()] = gdProperties[key].FromGodot();

		return properties;
	}
}
