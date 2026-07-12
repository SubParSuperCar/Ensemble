using System.Globalization;
using System.Numerics;
using Root.Core.Api.Asset;
using Root.Core.Impl.Util;

namespace Root.Core.Impl.Asset;

public class Instances : IInstances
{
	private readonly IAssets _assets;
	private readonly Counts<int> _countsByAssetId = new();
	private readonly HoleyArray<Instance> _instancesById = new();

	public Instances(IAssets assets, int? maxCount = null)
	{
		if (maxCount is { } count)
			ArgumentOutOfRangeException.ThrowIfNegative(count);

		_assets = assets;
		MaxCount = maxCount ?? Unlimited;

		_instancesById.Added += (id, instance) =>
		{
			instance.Id = id;
			Added?.Invoke(instance);
		};

		_instancesById.Removed += (_, instance) => Removed?.Invoke(instance);
	}

	public IEnumerable<IInstance> All => _instancesById.GetAll();

	public int Count => _countsByAssetId.Total;
	public int MaxCount { get; }

	public event Action<IInstance>? Added;
	public event Action<IInstance>? Removed;

	public bool TryGet(int instanceId, out IInstance instance)
	{
		if (!_instancesById.TryGet(instanceId, out var found))
		{
			instance = null!;
			return false;
		}

		instance = found;
		return true;
	}

	public IInstance Add(int assetId, Vector3 position, Quaternion rotation, int? instanceId = null)
	{
		if (instanceId is { } id)
			ArgumentOutOfRangeException.ThrowIfNegative(id);

		if (!_assets.All.TryGetValue(assetId, out var asset))
			throw new KeyNotFoundException(string.Create(
				CultureInfo.InvariantCulture,
				$"Asset with id {assetId} not found"));

		var instance = new Instance(asset, position, rotation);

		if (instanceId is { } slot)
			_instancesById.AddAt(instance, slot);
		else
			_instancesById.Add(instance);

		_countsByAssetId.Increment(assetId);
		return instance;
	}

	public void Remove(int instanceId)
	{
		if (!TryGet(instanceId, out var instance))
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Instance with id {instanceId} not found"));

		_instancesById.Remove(instanceId);
		_countsByAssetId.Decrement(instance.Asset.Id);
	}

	public void Clear()
	{
		foreach (var instance in _instancesById.GetAll().ToArray())
			Remove(instance.Id);
	}

	public Quota GetCount(int assetId) =>
		_assets.All.TryGetValue(assetId, out var asset)
			? (_countsByAssetId.Get(assetId), asset.MaxInstanceCount)
			: throw new KeyNotFoundException(string.Create(
				CultureInfo.InvariantCulture,
				$"Asset with id {assetId} not found"));

	public IReadOnlyDictionary<int, Quota> GetAllCounts()
	{
		var counts = new Dictionary<int, Quota>();

		foreach (var assetId in _assets.All.Keys)
			counts[assetId] = GetCount(assetId);

		return counts;
	}

	public IInstance GetInstance(int instanceId) =>
		TryGet(instanceId, out var instance)
			? instance
			: throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Instance with id {instanceId} not found"));
}
