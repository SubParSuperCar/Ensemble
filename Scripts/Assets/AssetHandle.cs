using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Assets;

[GlobalClass]
public partial class AssetHandle : RigidBody3D
{
	private bool _isBoundaryCalculated;

	[Export(PropertyHint.Range, "0,0,1,or_greater,hide_slider")]
	public int AssetId { get; set; }

	[Export] public StringName AssetName { get; set; } = null!;

	[Export] public Godot.Collections.Dictionary<StringName, Variant> Properties { get; set; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxInstanceCount { get; set; }

	// A zero size means unset; the flag keeps a calculated zero size (e.g., no collider) from recalculating
	[Export]
	public Aabb BoundaryAabb
	{
		get
		{
			if (_isBoundaryCalculated || field.Size != Vector3.Zero)
				return field;

			_isBoundaryCalculated = true;
			return field = CalculateBoundary();
		}
		set;
	}

	public int InstanceId { get; internal set; }

	private Aabb CalculateBoundary()
	{
		var collider = GetNodeOrNull<CollisionShape3D>("Collider");
		return collider?.Shape is { } shape ? collider.Transform * shape.GetDebugMesh().GetAabb() : default;
	}
}
