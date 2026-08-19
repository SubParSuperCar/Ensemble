using CoreRoot.Api.Assets;

namespace CoreRoot.Api.Plots;

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
