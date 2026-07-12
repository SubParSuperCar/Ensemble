using System.Numerics;

namespace Root.Core.Api.Asset;

public interface IInstances
{
	IEnumerable<IInstance> All { get; }

	int Count { get; }
	int MaxCount { get; }

	event Action<IInstance> Added;
	event Action<IInstance> Removed;

	bool TryGet(int instanceId, out IInstance instance);

	IInstance Add(int assetId, Vector3 position, Quaternion rotation, int? instanceId = null);

	void Remove(int instanceId);
	void Clear();

	Quota GetCount(int assetId);
	IReadOnlyDictionary<int, Quota> GetAllCounts();
}
