using System.Runtime.InteropServices;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.Replication.Actions;

/// <summary>An instance to add: its asset, its transform, and the properties it sets apart from the defaults.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct InstanceSpec(int AssetId, Vector3 Position, Quaternion Rotation, Dictionary Properties)
{
	public static InstanceSpec FromPayload(Array<Variant> payload) =>
		new(payload[0].AsInt32(), payload[1].AsVector3(), payload[2].AsQuaternion(), payload[3].AsGodotDictionary());

	public Array<Variant> ToPayload() => [AssetId, Position, Rotation, Properties];
}
