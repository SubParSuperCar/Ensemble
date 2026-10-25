using System.Runtime.InteropServices;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.Replication.Actions;

/// <summary>An instance and the properties to set on it.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct PropertyEdit(InstanceReference Instance, Dictionary Properties)
{
	public static PropertyEdit FromPayload(Array<Variant> payload) =>
		new(InstanceReference.FromPayload(payload[0].AsGodotArray<Variant>()), payload[1].AsGodotDictionary());

	public Array<Variant> ToPayload() => [Instance.ToPayload(), Properties];
}
