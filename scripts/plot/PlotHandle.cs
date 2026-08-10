using Godot;
using Root.Core.Gd.Asset;
using Root.Core.Gd.Plot;
using Root.Scripts.Asset;

namespace Root.Scripts.Plot;

[GlobalClass]
public partial class PlotHandle : Node3D
{
	private const float GridScale = 0.5f;

	private Basis _boundaryBasis;
	private Vector3 _boundarySize;
	private Node3D _instances = null!;
	private Vector3? _originPosition;
	private GdPlot _plot = null!;

	// ReSharper disable once MemberCanBePrivate.Global
	public Godot.Collections.Dictionary<int, AssetHandle> InstanceHandles { get; } = [];

	[Export(PropertyHint.Range, "0,0,1,or_greater,hide_slider")]
	public int Id { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxOccupantCount { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxTotalInstanceCount { get; set; }

	public Vector3 OriginPosition => _originPosition ??= CalculateOriginPosition();

	public override void _Ready()
	{
		_plot = GPlots.Get(Id)!;
		_instances = GetNode<Node3D>("Instances/Static");

		var boundary = GetNode<CollisionShape3D>("Boundary/Definition");
		_boundaryBasis = boundary.GlobalBasis;
		_boundarySize = boundary.Shape.GetDebugMesh().GetAabb().Size;

		foreach (var instance in _plot.Instances.GetAll())
			OnInstanceAdded(instance);

		_plot.Instances.Added += OnInstanceAdded;
		_plot.Instances.Removed += OnInstanceRemoved;
	}

	private void OnInstanceAdded(GdInstance instance)
	{
		var packed = GAssetManager.GetPacked(instance.Asset);

		var handle = packed.Instantiate<AssetHandle>();
		handle.InstanceId = instance.Id;

		_instances.AddChild(handle);

		handle.GlobalPosition = OriginPosition + instance.Position * GridScale;
		handle.Quaternion = instance.Rotation;

		InstanceHandles.Add(instance.Id, handle);
	}

	private void OnInstanceRemoved(GdInstance instance)
	{
		if (InstanceHandles.Remove(instance.Id, out var handle))
			handle.QueueFree();
	}

	private Vector3 CalculateOriginPosition()
	{
		var baseCollider = GetNode<CollisionShape3D>("Base/Collider");

		var position = baseCollider.GlobalPosition;
		var aabb = baseCollider.Shape.GetDebugMesh().GetAabb();
		position.Y += aabb.Size.Y / 2;

		return position;
	}
}
