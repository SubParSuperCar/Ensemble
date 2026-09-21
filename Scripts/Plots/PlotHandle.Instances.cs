using System.Globalization;
using Godot;
using Root.GdCore.Assets;
using Root.Scripts.Assets;

namespace Root.Scripts.Plots;

public partial class PlotHandle
{
	private Node3D _staticInstances = null!;

	public Godot.Collections.Dictionary<int, AssetHandle> InstanceHandles { get; } = [];

	private void ReadyInstances()
	{
		_staticInstances = GetNode<Node3D>("Instances/Static");

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
		handle.Transform = new Transform3D(new Basis(instance.Rotation), instance.Position * GridToWorldScale);
		handle.Freeze = true;

		_staticInstances.AddChild(handle);
		InstanceHandles.Add(instance.Id, handle);
	}

	private void OnInstanceRemoved(GdInstance instance)
	{
		if (InstanceHandles.Remove(instance.Id, out var handle))
			handle.QueueFree();
	}
}
