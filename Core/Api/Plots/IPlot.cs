using EnsembleCoreRoot.Api.Assets;

namespace EnsembleCoreRoot.Api.Plots;

/// <summary>
///     The representation of a buildable area containing <see cref="IOccupant" /> and <see cref="IInstance" />
///     objects, led by an owner authority, if any.
/// </summary>
public interface IPlot
{
	int Id { get; }

	/// <inheritdoc cref="IOccupants" />
	IOccupants Occupants { get; }

	/// <inheritdoc cref="IInstances" />
	IInstances Instances { get; }

	bool IsSpawned { get; }
	event Action<bool> IsSpawnedChanged;

	void Spawn();
	void Despawn();

	void Reset();
}
