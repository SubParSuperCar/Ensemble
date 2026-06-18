using System.Numerics;

namespace Root.Core.Api.Asset;

public interface IInstances
{
	IEnumerable<IInstance> All { get; }

	int Count { get; }
	int MaxCount { get; }

	event Action<IInstance> Added;
	event Action<IInstance> Removed;

	// ReSharper disable once UnusedMemberInSuper.Global
	bool TryGet(int instanceId, out IInstance instance);
	IInstance GetInstance(int instanceId);

	IInstance Add(int assetId, Vector3 position, Quaternion rotation, int? instanceId = null);

	void Remove(int instanceId);
	void Clear();

	(int Count, int MaxCount) GetCount(int assetId);
	IReadOnlyDictionary<int, (int Count, int MaxCount)> GetAllCounts();
}
