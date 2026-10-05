using System.Runtime.InteropServices;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.GdCore.Plots;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.Replication.Actions;

/// <summary>
///     Identifies an instance by ID and verifies it by asset and transform. Freed IDs are reused, so an action racing
///     a removal must not land on whichever instance took the ID next.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct InstanceReference(int Id, int AssetId, Vector3 Position, Quaternion Rotation)
{
	public static InstanceReference From(GdInstance instance) =>
		new(instance.Id, instance.Asset.Id, instance.Position, instance.Rotation);

	public static InstanceReference FromPayload(Array<Variant> payload) =>
		new(payload[0].AsInt32(), payload[1].AsInt32(), payload[2].AsVector3(), payload[3].AsQuaternion());

	public Array<Variant> ToPayload() => [Id, AssetId, Position, Rotation];

	public GdInstance? Resolve(GdPlot plot) =>
		plot.Instances.GetInstance(Id) is { } instance &&
		instance.Asset.Id == AssetId &&
		instance.Position == Position &&
		instance.Rotation == Rotation
			? instance
			: null;
}
