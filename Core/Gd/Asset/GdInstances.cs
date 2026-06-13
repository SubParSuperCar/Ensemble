using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Asset;
using Root.Core.Gd.Util;

namespace Root.Core.Gd.Asset;

[GlobalClass]
public partial class GdInstances : RefCounted
{
	[Signal]
	public delegate void AddedEventHandler(GdInstance instance);

	[Signal]
	public delegate void RemovedEventHandler(GdInstance instance);

	private static readonly ConditionalWeakTable<IInstances, GdInstances> Cache = [];
	private IInstances _instances = null!;

	public int Count => _instances.Count;
	public int MaxCount => _instances.MaxCount;

	public static GdInstances From(IInstances instances) => Cache.GetValue(instances,
		static i =>
		{
			var gdInstances = new GdInstances { _instances = i };

			i.Added += instance => gdInstances.EmitSignal(SignalName.Added, GdInstance.From(instance));
			i.Removed += instance => gdInstances.EmitSignal(SignalName.Removed, GdInstance.From(instance));

			return gdInstances;
		});

	public GdInstance Get(int id)
		=> GdInstance.From(_instances.GetInstance(id));

	public Array<GdInstance> GetAll()
	{
		var instances = new Array<GdInstance>();

		foreach (var instance in _instances.All)
			instances.Add(GdInstance.From(instance));

		return instances;
	}

	public GdInstance Add(int assetId, Vector3 position, Quaternion rotation)
		=> GdInstance.From(_instances.Add(assetId, position.FromGodot(), rotation.FromGodot()));

	public GdInstance AddAt(int assetId, Vector3 position, Quaternion rotation, int instanceId)
		=> GdInstance.From(_instances.Add(assetId, position.FromGodot(), rotation.FromGodot(), instanceId));

	public void Remove(int id) => _instances.Remove(id);
	public void Clear() => _instances.Clear();

	public Array<int> GetCount(int assetId)
	{
		var count = _instances.GetCount(assetId);
		return [count.Count, count.MaxCount];
	}

	public Godot.Collections.Dictionary<int, Array<int>> GetAllCounts()
	{
		var counts = new Godot.Collections.Dictionary<int, Array<int>>();

		foreach (var (key, value) in _instances.GetAllCounts())
			counts.Add(key, [value.Count, value.MaxCount]);

		return counts;
	}

	public Array<Dictionary> GetAllDicts()
	{
		var dicts = new Array<Dictionary>();

		foreach (var instance in _instances.All)
			dicts.Add(GdInstance.From(instance).ToDict());

		return dicts;
	}
}
