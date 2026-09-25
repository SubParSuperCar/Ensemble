using Godot;

namespace EnsembleRoot.Scripts.Assets;

[GlobalClass]
public partial class AssetHandle : RigidBody3D
{
	[Export(PropertyHint.Range, "0,0,1,or_greater,hide_slider")]
	public int AssetId { get; set; }

	[Export] public StringName AssetName { get; set; } = null!;

	[Export] public Godot.Collections.Dictionary<StringName, Variant> Properties { get; set; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxInstanceCount { get; set; }

	[Export]
	public Aabb BoundaryAabb
	{
		get
		{
			if (field.Size == Vector3.Zero)
				field = CalculateBoundary();

			return field;
		}
		set;
	}

	public int InstanceId { get; internal set; }

	private Aabb CalculateBoundary()
	{
		var collider = GetNodeOrNull<CollisionShape3D>("Collider");
		if (collider?.Shape is not { } shape)
			return default;

		var aabb = shape.GetDebugMesh().GetAabb();
		return aabb * collider.Transform;
	}
}
