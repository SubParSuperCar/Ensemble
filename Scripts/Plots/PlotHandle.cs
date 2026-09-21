using Godot;
using Root.GdCore.Plots;

namespace Root.Scripts.Plots;

[GlobalClass]
public partial class PlotHandle : Node3D
{
	public const float GridToWorldScale = 0.5f;

	private Transform3D? _originTransform;
	private GdPlot _plot = null!;

	[Export(PropertyHint.Range, "0,0,1,or_greater,hide_slider")]
	public int Id { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxOccupantCount { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxTotalInstanceCount { get; set; }

	public Transform3D OriginTransform => _originTransform ??= CalculateOriginTransform();

	public Transform3D BoundaryTransform { get; private set; }
	public Vector3 BoundarySize { get; private set; }

	public Vector3 GridBoundarySize { get; private set; }

	public override void _Ready()
	{
		_plot = GPlots.GetPlot(Id)!;

		var boundary = GetNode<CollisionShape3D>("Boundary/Definition");
		BoundaryTransform = boundary.GlobalTransform;
		BoundarySize = boundary.Shape.GetDebugMesh().GetAabb().Size;

		GridBoundarySize = BoundarySize / GridToWorldScale;

		ReadyInstances();
	}

	public Vector3 WorldToGrid(Vector3 worldPosition) =>
		OriginTransform.AffineInverse() * worldPosition / GridToWorldScale;

	public Transform3D GridToWorld(Vector3 gridPosition, Quaternion rotation) =>
		OriginTransform * new Transform3D(new Basis(rotation), gridPosition * GridToWorldScale);

	private Transform3D CalculateOriginTransform()
	{
		var baseCollider = GetNode<CollisionShape3D>("Base/Collider");

		var aabb = baseCollider.Shape.GetDebugMesh().GetAabb();
		var position = baseCollider.GlobalTransform * aabb.GetCenter();

		return new Transform3D(baseCollider.GlobalBasis, position);
	}
}
