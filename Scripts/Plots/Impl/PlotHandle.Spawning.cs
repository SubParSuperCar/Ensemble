using System.Diagnostics;
using EnsembleRoot.Scripts.Creations;
using EnsembleRoot.Scripts.Plots.Placement;
using Godot;
using Serilog;

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	private const int MaxLiftPasses = 64;

	private readonly List<Obb> _liftCandidates = [];
	private readonly PlacementSolver _solver = new();

	private Node3D _dynamicInstances = null!;

	/// <summary>The creation while the plot is spawned, else null.</summary>
	public LiveCreation? Creation { get; private set; }

	private void ReadySpawning()
	{
		_dynamicInstances = GetNode<Node3D>("Instances/Dynamic");

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

	private void Spawn()
	{
		var timestamp = Stopwatch.GetTimestamp();

		SetStaticInstancesEnabled(false);
		SimulationEpoch++;

		Creation = new LiveCreation { Name = "Creation", Plot = this, Origin = _staticInstances.GlobalTransform };
		_dynamicInstances.AddChild(Creation);
		Creation.Build(_plot.Instances.GetAll());

		MoveLocalCharacterClear();
		_sinceSimulatorHeard = 0;

		Log.Debug(
			"Spawned plot {PlotId} as {AssemblyCount} assemblies in {ElapsedMs:F3} ms", Id, Creation.Assemblies.Count,
			Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);
	}

	private void Despawn()
	{
		if (Creation is { } creation)
		{
			creation.WakeNeighbors();
			creation.QueueFree();
			Creation = null;
		}

		GPlotManager.ForgetArbitration(this);

		// The host announces the hand-off before peers despawn, so it must carry the epoch they still hold
		ArbitratedSimulatorPeerId = null;
		ClearSimulatorOverride();
		SimulationEpoch++;

		SetStaticInstancesEnabled(true);

		Log.Debug("Despawned plot {PlotId}", Id);
	}

	private void SetStaticInstancesEnabled(bool isEnabled)
	{
		_staticInstances.ProcessMode = isEnabled ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
		_staticInstances.Visible = isEnabled;
	}

	// Placement ignores characters, so the creation may spawn around one; each peer moves its own, before physics does
	private void MoveLocalCharacterClear()
	{
		if (GPlayerManager.LocalHandle is not { } player)
			return;

		var body = player.Body;
		var origin = _staticInstances.GlobalTransform;
		var box = Obb.From(player.BodyBounds, origin.AffineInverse() * body.GlobalTransform);

		if (!InstanceBoxes.Overlaps(box))
			return;

		var cleared = LiftClear(_solver.Settle(box, InstanceBoxes, float.NegativeInfinity, Vector3.Up, null, default));
		player.Teleport(body.GlobalPosition + origin.Basis * (cleared.Center - box.Center));
	}

	// Rises above whatever the sideways push couldn't escape, e.g., inside a closed shell
	private Obb LiftClear(Obb box)
	{
		for (var pass = 0; pass < MaxLiftPasses && InstanceBoxes.Overlaps(box); pass++)
		{
			InstanceBoxes.Query(box, _liftCandidates);

			var lift = _liftCandidates
				.Where(box.Overlaps)
				.Max(obstacle => box.GetSeparationDistance(obstacle, Vector3.Up));

			box = box.At(box.Center + Vector3.Up * lift);
		}

		return box;
	}
}
