using Godot;

namespace Root.Scripts.Plot;

public partial class PlotHandle : Node3D
{
	private Vector3? _spawnLocation;

	[Export(PropertyHint.Range, "0,0,1,or_greater,hide_slider")]
	public int Id { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxOccupantCount { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxTotalInstanceCount { get; set; }

	public Vector3 SpawnLocation => _spawnLocation ??= CalculateSpawnLocation();

	private Vector3 CalculateSpawnLocation()
	{
		var collider = GetNode<CollisionShape3D>("Base/Collider");

		var position = collider.GlobalPosition;
		var aabb = collider.Shape.GetDebugMesh().GetAabb();
		position.Y += aabb.Size.Y / 2;

		return position;
	}
}
