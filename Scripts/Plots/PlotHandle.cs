using System.Globalization;
using Godot;
using Root.GdCore.Assets;
using Root.GdCore.Plots;
using Root.Scripts.Assets;

namespace Root.Scripts.Plots;

[GlobalClass]
public partial class PlotHandle : Node3D
{
	public const float GridToWorldScale = 0.5f;

	private Transform3D? _originTransform;
	private GdPlot _plot = null!;
	private Node3D _staticInstances = null!;

	public Godot.Collections.Dictionary<int, AssetHandle> InstanceHandles { get; } = [];

	[Export(PropertyHint.Range, "0,0,1,or_greater,hide_slider")]
	public int Id { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxOccupantCount { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxTotalInstanceCount { get; set; }

	public Transform3D OriginTransform => _originTransform ??= CalculateOriginTransform();

	public Basis GridBoundaryBasis { get; private set; }
	public Vector3 GridBoundarySize { get; private set; }

	public Transform3D BoundaryTransform { get; private set; }
	public Vector3 BoundarySize { get; private set; }

	public override void _Ready()
	{
		_plot = GPlots.GetPlot(Id)!;
		_staticInstances = GetNode<Node3D>("Instances/Static");

		var boundary = GetNode<CollisionShape3D>("Boundary/Definition");
		BoundaryTransform = boundary.GlobalTransform;
		BoundarySize = boundary.Shape.GetDebugMesh().GetAabb().Size;

		GridBoundaryBasis = boundary.GlobalBasis;
		GridBoundarySize = BoundarySize / GridToWorldScale;

		foreach (var instance in _plot.Instances.GetAll())
			OnInstanceAdded(instance);

		_plot.Instances.Added += OnInstanceAdded;
		_plot.Instances.Removed += OnInstanceRemoved;
	}

	private void OnInstanceAdded(GdInstance instance)
	{
		var packed = GAssetManager.GetPacked(instance.Asset.Id);

		var handle = packed.Instantiate<AssetHandle>();
		handle.Name = string.Create(CultureInfo.InvariantCulture, $"{instance.Id}-{instance.Asset.Name}");
		handle.InstanceId = instance.Id;
		handle.Freeze = true;

		_staticInstances.AddChild(handle);

		var transform = new Transform3D(
			new Basis(instance.Rotation),
			instance.Position * GridToWorldScale);

		handle.GlobalTransform = OriginTransform * transform;
		InstanceHandles.Add(instance.Id, handle);
	}

	private void OnInstanceRemoved(GdInstance instance)
	{
		if (InstanceHandles.Remove(instance.Id, out var handle))
			handle.QueueFree();
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
