using System.Globalization;
using System.Numerics;
using Root.Core.Api.Asset;
using Root.Core.Impl.Util;

namespace Root.Core.Impl.Asset;

public class Instances : IInstances
{
	private readonly IAssets _assets;
	private readonly HoleyArray<Instance> _byId = new();
	private readonly Counts<int> _perAsset = new();

	public Instances(IAssets assets, int? maxCount = null)
	{
		if (maxCount is { } count)
			ArgumentOutOfRangeException.ThrowIfNegative(count);

		_assets = assets;
		MaxCount = maxCount ?? Unlimited;

		_byId.Added += (id, instance) =>
		{
			instance.Id = id;
			Added?.Invoke(instance);
		};

		_byId.Removed += (_, instance) => Removed?.Invoke(instance);
	}

	public IEnumerable<IInstance> All => _byId.GetAll();

	public int Count => _perAsset.Total;
	public int MaxCount { get; }

	public event Action<IInstance>? Added;
	public event Action<IInstance>? Removed;

	public bool TryGet(int instanceId, out IInstance instance)
	{
		if (!_byId.TryGet(instanceId, out var found))
		{
			instance = null!;
			return false;
		}

		instance = found;
		return true;
	}

	public IInstance GetInstance(int instanceId)
		=> TryGet(instanceId, out var instance)
			? instance
			: throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Instance with id {instanceId} not found"));

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
			_byId.AddAt(instance, slot);
		else
			_byId.Add(instance);

		_perAsset.Increment(assetId);
		return instance;
	}

	public void Remove(int instanceId)
	{
		if (!TryGet(instanceId, out var instance))
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Instance with id {instanceId} not found"));

		_byId.Remove(instanceId);
		_perAsset.Decrement(instance.Asset.Id);
	}

	public void Clear()
	{
		foreach (var instance in _byId.GetAll().ToArray())
			Remove(instance.Id);
	}

	public (int Count, int MaxCount) GetCount(int assetId)
		=> _assets.All.TryGetValue(assetId, out var asset)
			? (_perAsset.Get(assetId), asset.MaxInstanceCount)
			: throw new KeyNotFoundException(string.Create(
				CultureInfo.InvariantCulture,
				$"Asset with id {assetId} not found"));

	public IReadOnlyDictionary<int, (int Count, int MaxCount)> GetAllCounts()
	{
		var counts = new Dictionary<int, (int Count, int MaxCount)>();

		foreach (var assetId in _assets.All.Keys)
			counts[assetId] = GetCount(assetId);

		return counts;
	}
}
