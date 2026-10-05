using System.Globalization;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Plots.Impl;
using Godot;

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	// We're only syncing despawned AssetHandle objects for now. Spawning/despawning will come soon(TM).
	private readonly Dictionary<int, Obb> _instanceBoxes = [];
	private Node3D _staticInstances = null!;

	public Godot.Collections.Dictionary<int, AssetHandle> InstanceHandles { get; } = [];

	// Cached since reading every handle back from Godot each physics tick is far slower than testing them
	internal IReadOnlyCollection<Obb> InstanceBoxes => _instanceBoxes.Values;

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
		var packed = GAssetManager.GetPacked(instance.Asset.Id);

		var handle = packed.Instantiate<AssetHandle>();
		handle.Name = string.Create(CultureInfo.InvariantCulture, $"{instance.Id}-{instance.Asset.Name}");
		handle.InstanceId = instance.Id;
		handle.Transform = new Transform3D(new Basis(instance.Rotation), instance.Position * GridToWorldScale);
		handle.Freeze = true;

		_staticInstances.AddChild(handle);
		InstanceHandles.Add(instance.Id, handle);
		_instanceBoxes.Add(instance.Id, Obb.From(handle.BoundaryAabb, handle.Transform));
	}

	private void OnInstanceRemoved(GdInstance instance)
	{
		_instanceBoxes.Remove(instance.Id);

		if (InstanceHandles.Remove(instance.Id, out var handle))
			handle.QueueFree();
	}
}
