using Root.Core.Api.Asset;

namespace Root.Core.Api.Plot;

public interface IPlot
{
	int Id { get; }

	IOccupants Occupants { get; }
	IInstances Instances { get; }

	bool IsSpawned { get; }
	event Action<bool> IsSpawnedChanged;

	void Spawn();
	void Despawn();

	void Reset();
}
