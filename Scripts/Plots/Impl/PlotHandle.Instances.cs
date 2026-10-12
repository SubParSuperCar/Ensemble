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

	// Cached, as reading every handle back from Godot each tick is slow
	internal ObbGrid InstanceBoxes { get; } = new();

	private void ReadyInstances()
	{
		_staticInstances = GetNode<Node3D>("Instances/Static");

		foreach (var instance in _plot.Instances.GetAll())
			OnInstanceAdded(instance);

		_plot.Instances.Added += OnInstanceAdded;
		_plot.Instances.Removed += OnInstanceRemoved;
	}

	private void ExitInstances()
	{
		_plot.Instances.Added -= OnInstanceAdded;
		_plot.Instances.Removed -= OnInstanceRemoved;
	}

	private void OnInstanceAdded(GdInstance instance)
	{
		var transform = new Transform3D(new Basis(instance.Rotation), instance.Position * GridToWorldScale);

		var handle = GAssetManager.GetPacked(instance.Asset.Id).Instantiate<AssetHandle>();
		handle.Name = string.Create(CultureInfo.InvariantCulture, $"{instance.Id}-{instance.Asset.Name}");
		handle.InstanceId = instance.Id;
		handle.Transform = transform;
		handle.Freeze = true;

		_staticInstances.AddChild(handle);
		_handlesByInstanceId.Add(instance.Id, handle);
		InstanceBoxes.Add(instance.Id, Obb.From(handle.BoundaryAabb, transform));
	}

	private void OnInstanceRemoved(GdInstance instance)
	{
		InstanceBoxes.Remove(instance.Id);

		if (_handlesByInstanceId.Remove(instance.Id, out var handle))
			handle.QueueFree();
	}
}
