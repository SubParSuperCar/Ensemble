using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using EnsembleCoreRoot.Api.Assets;
using EnsembleCoreRoot.Utils;

namespace EnsembleCoreRoot.Assets;

/// <inheritdoc />
public class Instances : IInstances
{
	private readonly IAssets _assets;
	private readonly Counts<int> _countsByAssetId = new();
	private readonly HoleyArray<Instance> _instancesById = new();

	public Instances(IAssets assets, int? maxCount = null)
	{
		if (maxCount is { } count and not Unlimited)
			ArgumentOutOfRangeException.ThrowIfNegative(count);

		_assets = assets;
		MaxCount = maxCount ?? Unlimited;

		_instancesById.Added += (id, instance) =>
		{
			instance.Id = id;
			_countsByAssetId.Increment(instance.Asset.Id);

			Added?.Invoke(instance);
		};

		_instancesById.Removed += (_, instance) =>
		{
			_countsByAssetId.Decrement(instance.Asset.Id);
			Removed?.Invoke(instance);
		};
	}

	public IEnumerable<IInstance> All => _instancesById.GetAll();

	public int Count => _countsByAssetId.Total;
	public int MaxCount { get; }

	public event Action<IInstance>? Added;
	public event Action<IInstance>? Removed;

	public bool TryGet(int instanceId, [NotNullWhen(true)] out IInstance? instance)
	{
		var isFound = _instancesById.TryGet(instanceId, out var found);
		instance = found;

		return isFound;
	}

	public IInstance Add(int assetId, Vector3 position, Quaternion rotation, int? instanceId = null)
	{
		if (instanceId is { } id)
			ArgumentOutOfRangeException.ThrowIfNegative(id);

		if (!_assets.All.TryGetValue(assetId, out var asset))
			throw new KeyNotFoundException(string.Create(
				CultureInfo.InvariantCulture,
				$"Asset with id {assetId} not found."));

		var instance = new Instance(asset, position, rotation);

		if (instanceId is { } slot)
			_instancesById.AddAt(instance, slot);
		else
			_instancesById.Add(instance);

		return instance;
	}

	public void Remove(int instanceId)
	{
		if (!TryGet(instanceId, out _))
			throw new KeyNotFoundException(string.Create(
				CultureInfo.InvariantCulture,
				$"Instance with id {instanceId} not found."));

		_instancesById.Remove(instanceId);
	}

	public void Clear()
	{
		foreach (var instance in _instancesById.GetAll().ToArray())
			Remove(instance.Id);
	}

	public Quota GetQuota(int assetId) =>
		_assets.All.TryGetValue(assetId, out var asset)
			? (_countsByAssetId.Get(assetId), asset.MaxInstanceCount)
			: throw new KeyNotFoundException(string.Create(
				CultureInfo.InvariantCulture,
				$"Asset with id {assetId} not found."));

	public IReadOnlyDictionary<int, Quota> GetAllQuotas() => _assets.All.Keys.ToDictionary(static id => id, GetQuota);
}
