using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using EnsembleCoreRoot.Api.Plots;

namespace EnsembleCoreRoot.Api.Assets;

/// <summary>
///     The <see cref="IInstance" /> objects placed on a single <see cref="IPlot" />.
/// </summary>
public interface IInstances
{
	/// <summary>
	///     The most instances any plot holds, whatever its <see cref="MaxCount" />. IDs are taken lowest-free and stay
	///     below it, which bounds the memory a plot's IDs can claim (e.g., from a snapshot); saves hold no more.
	/// </summary>
	const int MaxPossibleCount = 1 << 20;

	IEnumerable<IInstance> All { get; }

	int Count { get; }
	int MaxCount { get; }

	event Action<IInstance> Added;
	event Action<IInstance> Removed;

	bool TryGet(int instanceId, [NotNullWhen(true)] out IInstance? instance);

	IInstance Add(int assetId, Vector3 position, Quaternion rotation, int? instanceId = null);

	void Remove(int instanceId);
	void Clear();

	Quota GetQuota(int assetId);
	IReadOnlyDictionary<int, Quota> GetAllQuotas();
}
