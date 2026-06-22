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

	private static readonly ConditionalWeakTable<IInstances, GdInstances> Wrappers = [];
	private IInstances _source = null!;

	public int Count => _source.Count;
	public int MaxCount => _source.MaxCount;

	public static GdInstances From(IInstances instances) =>
		Wrappers.GetValue(instances,
			static source =>
			{
				var wrapper = new GdInstances { _source = source };

				source.Added += instance => wrapper.EmitSignal(SignalName.Added, GdInstance.From(instance));
				source.Removed += instance => wrapper.EmitSignal(SignalName.Removed, GdInstance.From(instance));

				return wrapper;
			});

	public GdInstance Get(int id) => GdInstance.From(_source.GetInstance(id));

	public Array<GdInstance> GetAll()
	{
		var result = new Array<GdInstance>();

		foreach (var instance in _source.All)
			result.Add(GdInstance.From(instance));

		return result;
	}

	public GdInstance Add(int assetId, Vector3 position, Quaternion rotation) =>
		GdInstance.From(_source.Add(assetId, position.FromGodot(), rotation.FromGodot()));

	public GdInstance AddAt(int assetId, Vector3 position, Quaternion rotation, int instanceId) =>
		GdInstance.From(_source.Add(assetId, position.FromGodot(), rotation.FromGodot(), instanceId));

	public void Remove(int id) => _source.Remove(id);
	public void Clear() => _source.Clear();

	public Array<int> GetCount(int assetId)
	{
		var (count, maxCount) = _source.GetCount(assetId);
		return [count, maxCount];
	}

	public Godot.Collections.Dictionary<int, Array<int>> GetAllCounts()
	{
		var result = new Godot.Collections.Dictionary<int, Array<int>>();

		foreach (var (assetId, count) in _source.GetAllCounts())
			result.Add(assetId, [count.Count, count.MaxCount]);

		return result;
	}

	public Array<Dictionary> GetAllDicts()
	{
		var result = new Array<Dictionary>();

		foreach (var instance in _source.All)
			result.Add(GdInstance.From(instance).ToDict());

		return result;
	}
}
