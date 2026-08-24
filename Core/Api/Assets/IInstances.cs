using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace CoreRoot.Api.Assets;

public interface IInstances
{
	IEnumerable<IInstance> All { get; }

	int Count { get; }
	int MaxCount { get; }

	event Action<IInstance> Added;
	event Action<IInstance> Removed;

	bool TryGet(int instanceId, [NotNullWhen(true)] out IInstance? instance);

	IInstance Add(int assetId, Vector3 position, Quaternion rotation, int? instanceId = null);

	void Remove(int instanceId);
	void Clear();

	Quota GetCount(int assetId);
	IReadOnlyDictionary<int, Quota> GetAllCounts();
}
