using System.Globalization;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Plots.Impl;
using Godot;

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	private readonly Dictionary<int, AssetHandle> _handlesByInstanceId = [];

	private Node3D _staticInstances = null!;

	// The boxes are cached, as reading every handle back from Godot each tick is slow
	internal ObbGrid InstanceBoxes { get; } = new();

	public AssetHandle? GetInstanceHandle(int instanceId) => _handlesByInstanceId.GetValueOrDefault(instanceId);

	private void ReadyInstances()
	{
		_staticInstances = GetNode<Node3D>("Instances/Static");

		var instances = _plot.Instances.GetAll();

		foreach (var instance in instances)
			AddStaticInstance(instance);

		foreach (var instance in instances)
			_handlesByInstanceId[instance.Id].Initialize(this, instance);

		_plot.Instances.Added += OnInstanceAdded;
		_plot.Instances.Removed += OnInstanceRemoved;
	}

	private void ExitInstances()
	{
		_plot.Instances.Added -= OnInstanceAdded;
		_plot.Instances.Removed -= OnInstanceRemoved;
	}

	private static Transform3D GetTransform(GdInstance instance) =>
		new(new Basis(instance.Rotation), instance.Position * GridToWorldScale);

	private static AssetHandle CreateHandle(GdInstance instance, Transform3D transform)
	{
		var handle = GAssetManager.GetPacked(instance.Asset.Id).Instantiate<AssetHandle>();
		handle.Name = string.Create(CultureInfo.InvariantCulture, $"{instance.Id}-{instance.Asset.Name}");
		handle.InstanceId = instance.Id;
		handle.Transform = transform;

		return handle;
	}

	private void OnInstanceAdded(GdInstance instance) => AddStaticInstance(instance).Initialize(this, instance);

	private void OnInstanceRemoved(GdInstance instance)
	{
		InstanceBoxes.Remove(instance.Id);

		if (_handlesByInstanceId.Remove(instance.Id, out var handle))
			handle.QueueFree();
	}

	// The static instances mirror the plot even while it's spawned (hidden, out of the physics space)
	private AssetHandle AddStaticInstance(GdInstance instance)
	{
		var transform = GetTransform(instance);

		var handle = CreateHandle(instance, transform);
		handle.Freeze = true;
		handle.CollisionLayer = PhysicsLayers.StaticInstances;
		handle.CollisionMask = 0;

		_staticInstances.AddChild(handle);
		_handlesByInstanceId.Add(instance.Id, handle);
		InstanceBoxes.Add(instance.Id, Obb.From(GAssetManager.GetBoundary(instance.Asset.Id), transform));

		return handle;
	}
}
