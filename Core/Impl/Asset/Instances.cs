using System.Globalization;
using System.Numerics;
using Root.Core.Api.Asset;
using Root.Core.Impl.Util;

namespace Root.Core.Impl.Asset;

public class Instances : IInstances
{
	private readonly IAssets _assets;
	private readonly Counts<int> _counts = new();
	private readonly HoleyArray<Instance> _instances = new();

	public Instances(IAssets assets, int? maxCount = null)
	{
		if (maxCount.HasValue)
			ArgumentOutOfRangeException.ThrowIfNegative(maxCount.Value);

		_assets = assets;
		MaxCount = maxCount ?? Unlimited;

		_instances.Added += (id, instance) =>
		{
			instance.Id = id;
			Added?.Invoke(instance);
		};

		_instances.Removed += (_, instance) => Removed?.Invoke(instance);
	}

	public IEnumerable<IInstance> All => _instances.GetAll();

	public int Count => _counts.Total;
	public int MaxCount { get; }

	public event Action<IInstance>? Added;
	public event Action<IInstance>? Removed;

	public bool TryGet(int instanceId, out IInstance instance)
	{
		if (_instances.TryGet(instanceId, out var concrete))
		{
			instance = concrete;
			return true;
		}

		instance = null!;
		return false;
	}

	public IInstance GetInstance(int instanceId)
		=> TryGet(instanceId, out var instance)
			? instance
			: throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Instance with id {instanceId} not found"));

	public IInstance Add(int assetId, Vector3 position, Quaternion rotation, int? instanceId = null)
	{
		if (instanceId.HasValue)
			ArgumentOutOfRangeException.ThrowIfNegative(instanceId.Value);

		if (!_assets.All.TryGetValue(assetId, out var asset))
			throw new KeyNotFoundException(string.Create(
				CultureInfo.InvariantCulture,
				$"Asset with id {assetId} not found"));

		var instance = new Instance(asset, position, rotation);

		if (instanceId is { } id)
			_instances.AddAt(instance, id);
		else
			_instances.Add(instance);

		_counts.Increment(assetId);
		return instance;
	}

	public void Remove(int instanceId)
	{
		if (!TryGet(instanceId, out var instance))
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Instance with id {instanceId} not found"));

		_instances.Remove(instanceId);
		_counts.Decrement(instance.Asset.Id);
	}

	public void Clear()
	{
		foreach (var instance in _instances.GetAll().ToArray())
			Remove(instance.Id);
	}

	public (int Count, int MaxCount) GetCount(int assetId)
		=> _assets.All.TryGetValue(assetId, out var asset)
			? (_counts.Get(assetId), asset.MaxInstanceCount)
			: throw new KeyNotFoundException(string.Create(
				CultureInfo.InvariantCulture,
				$"Asset with id {assetId} not found"));

	public IReadOnlyDictionary<int, (int Count, int MaxCount)> GetAllCounts()
	{
		var counts = new Dictionary<int, (int, int)>();

		foreach (var id in _assets.All.Keys)
			counts[id] = GetCount(id);

		return counts;
	}
}
