using Godot;
using Root.GdCore.Plots;
using Root.Scripts.Assets;

namespace Root.Scripts.Plots;

[GlobalClass]
public partial class PlotHandle : Node3D
{
	private Node3D _staticInstances = null!;
	private Transform3D? _originTransform;
	private GdPlot _plot = null!;

	public Godot.Collections.Dictionary<int, AssetHandle> InstanceHandles { get; } = [];

	[Export(PropertyHint.Range, "0,0,1,or_greater,hide_slider")]
	public int Id { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxOccupantCount { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxTotalInstanceCount { get; set; }

	public Transform3D OriginTransform => _originTransform ??= CalculateOriginTransform();

	public Basis BoundaryBasis { get; private set; }
	public Vector3 BoundarySize { get; private set; }

	public override void _Ready()
	{
		_plot = GPlots.GetPlot(Id)!;
		_staticInstances = GetNode<Node3D>("Instances/Static");

		var boundary = GetNode<CollisionShape3D>("Boundary/Definition");
		BoundaryBasis = boundary.GlobalBasis;
		BoundarySize = boundary.Shape.GetDebugMesh().GetAabb().Size;
	}

	private Transform3D CalculateOriginTransform()
	{
		var baseCollider = GetNode<CollisionShape3D>("Base/Collider");

		var position = baseCollider.GlobalPosition;
		var aabb = baseCollider.Shape.GetDebugMesh().GetAabb();
		position.Y += aabb.Size.Y / 2;

		return new Transform3D(baseCollider.GlobalBasis, position);
	}
}
