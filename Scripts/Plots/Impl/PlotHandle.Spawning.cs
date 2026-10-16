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

	// TODO: Replace the static instances with a live, simulated creation (under Instances/Dynamic)
	private void Spawn()
	{
		ClearStaticInstances();
		Log.Debug("Spawned plot {PlotId}", Id);
	}

	// TODO: Free the live creation before the static instances are restored
	private void Despawn()
	{
		AddStaticInstances();
		Log.Debug("Despawned plot {PlotId}", Id);
	}
}
