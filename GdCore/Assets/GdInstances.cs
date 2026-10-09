using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.GdCore.Utils;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.GdCore.Assets;

/// <inheritdoc cref="IInstances" />
public partial class GdInstances : RefCounted
{
	[Signal]
	public delegate void AddedEventHandler(GdInstance instance);

	[Signal]
	public delegate void RemovedEventHandler(GdInstance instance);

	private static readonly ConditionalWeakTable<IInstances, GdInstances> Wrappers = [];

	/// <inheritdoc cref="IInstances" />
	public IInstances Source { get; private init; } = null!;

	public int Count => Source.Count;
	public int MaxCount => Source.MaxCount;

	public static GdInstances From(IInstances instances) =>
		Wrappers.GetValue(instances,
			static source =>
			{
				var wrapper = new GdInstances { Source = source };

				source.Added += instance => wrapper.EmitSignal(SignalName.Added, GdInstance.From(instance));
				source.Removed += instance => wrapper.EmitSignal(SignalName.Removed, GdInstance.From(instance));

				return wrapper;
			});

	public GdInstance? GetInstance(int id) => Source.TryGet(id, out var instance) ? GdInstance.From(instance) : null;

	public Array<GdInstance> GetAll() => [.. Source.All.Select(GdInstance.From)];

	public GdInstance Add(int assetId, Vector3 position, Quaternion rotation) =>
		GdInstance.From(Source.Add(assetId, position.FromGodot(), rotation.FromGodot()));

	public GdInstance AddAt(int assetId, Vector3 position, Quaternion rotation, int instanceId) =>
		GdInstance.From(Source.Add(assetId, position.FromGodot(), rotation.FromGodot(), instanceId));

	public void Remove(int id) => Source.Remove(id);
	public void Clear() => Source.Clear();

	public Array<int> GetQuota(int assetId)
	{
		var (count, maxCount) = Source.GetQuota(assetId);
		return [count, maxCount];
	}

	public Godot.Collections.Dictionary<int, Array<int>> GetAllQuotas()
	{
		var result = new Godot.Collections.Dictionary<int, Array<int>>();

		foreach (var (assetId, quota) in Source.GetAllQuotas())
			result.Add(assetId, [quota.Count, quota.MaxCount]);

		return result;
	}

	public Array<Dictionary> GetAllDicts() =>
		[.. Source.All.Select(static instance => GdInstance.From(instance).ToDict())];
}
