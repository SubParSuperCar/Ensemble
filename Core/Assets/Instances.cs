using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using EnsembleCoreRoot.Api.Assets;
using EnsembleCoreRoot.Utils;

namespace EnsembleCoreRoot.Assets;

/// <inheritdoc />
public sealed class Instances : IInstances
{
	private readonly IAssets _assets;
	private readonly Dictionary<int, int> _countsByAssetId = [];
	private readonly HoleyArray<Instance> _instancesById = new();

	public Instances(IAssets assets, int? maxCount = null)
	{
		ThrowIfInvalidMaxCount(maxCount);

		_assets = assets;
		MaxCount = maxCount ?? Unlimited;

		_instancesById.Added += (id, instance) =>
		{
			instance.Id = id;
			CollectionsMarshal.GetValueRefOrAddDefault(_countsByAssetId, instance.Asset.Id, out _)++;
			Added?.Invoke(instance);
		};

		_instancesById.Removed += (_, instance) =>
		{
			_countsByAssetId[instance.Asset.Id]--;
			Removed?.Invoke(instance);
		};
	}

	public IEnumerable<IInstance> All => _instancesById.GetAll();

	public int Count => _instancesById.Count;
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
		var instance = new Instance(GetAsset(assetId), position, rotation);

		if (instanceId is { } id)
			_instancesById.AddAt(instance, id);
		else
			_instancesById.Add(instance);

		return instance;
	}

	public void Remove(int instanceId)
	{
		if (!_instancesById.Remove(instanceId))
			throw new KeyNotFoundException(
				string.Create(CultureInfo.InvariantCulture, $"Instance with ID {instanceId} not found."));
	}

	public void Clear()
	{
		foreach (var instance in _instancesById.GetAll().ToArray())
			_instancesById.Remove(instance.Id);
	}

	public Quota GetQuota(int assetId) =>
		(_countsByAssetId.GetValueOrDefault(assetId), GetAsset(assetId).MaxInstanceCount);

	public IReadOnlyDictionary<int, Quota> GetAllQuotas() => _assets.All.Keys.ToDictionary(static id => id, GetQuota);

	private IAsset GetAsset(int assetId) =>
		_assets.All.TryGetValue(assetId, out var asset)
			? asset
			: throw new KeyNotFoundException(
				string.Create(CultureInfo.InvariantCulture, $"Asset with ID {assetId} not found."));
}
