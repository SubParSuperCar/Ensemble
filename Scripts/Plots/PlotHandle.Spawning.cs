using Serilog;

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	private void ReadySpawning()
	{
		if (_plot.IsSpawned)
			Spawn();

		_plot.IsSpawnedChanged += OnIsSpawnedChanged;
	}

	private void ExitSpawning() => _plot.IsSpawnedChanged -= OnIsSpawnedChanged;

	private void OnIsSpawnedChanged(bool isSpawned)
	{
		if (isSpawned)
			Spawn();
		else
			Despawn();
	}

	// TODO: Spawn the static instances as a live, simulated creation (under Instances/Dynamic)
	private void Spawn() => Log.Debug("Spawned plot {PlotId} (not implemented)", Id);

	// TODO: Despawn the live creation, restoring the static instances
	private void Despawn() => Log.Debug("Despawned plot {PlotId} (not implemented)", Id);
}
