using System.Diagnostics;
using EnsembleRoot.Scripts.Plots.Impl;
using Godot;
using Serilog;

// ReSharper disable UnusedMember.Global

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	private const float KillHeight = -1024f;
	private const double KillCheckInterval = 1d;
	private const int MaxLiftPasses = 64;

	// Past the creation's bounds, so bodies resting on its surface count as touching it
	private const float WakeMargin = 1 / 8f;

	private readonly List<Obb> _liftCandidates = [];
	private readonly PlacementSolver _solver = new();

	private double _sinceKillCheck;

	private void ReadySpawning()
	{
		_dynamicInstances = GetNode<Node3D>("Instances/Dynamic");
		SetPhysicsProcess(false);

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
		BuildCreation();

		MoveLocalCharacterClear();

		_sinceKillCheck = 0;
		_sinceSimulatorHeard = 0;
		SetPhysicsProcess(true);

		Log.Debug(
			"Spawned plot {PlotId} as {AssemblyCount} assemblies in {ElapsedMs:F3} ms", Id, _assemblies.Count,
			Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);

		// Streamed states carry 16-bit IDs, which only creations far beyond any plot's quota exceed
		if (IsStreaming && _assemblies.Count > AssemblyState.MaxId + 1)
			Log.Warning(
				"Plot {PlotId} has {Count} assemblies, and other peers won't see those past the first {MaxCount} move",
				Id, _assemblies.Count, AssemblyState.MaxId + 1);
	}

	private void Despawn()
	{
		SetPhysicsProcess(false);

		// The host announces the hand-off before peers despawn, so it must carry the epoch they still hold
		ArbitratedSimulatorPeerId = null;
		ClearSimulatorOverride();
		SimulationEpoch++;

		GPlotManager.ForgetArbitration(this);
		WakeNeighbors();

		foreach (var assembly in _assemblies)
			assembly.QueueFree();

		_assemblies.Clear();
		ResetReplication();
		SetStaticInstancesEnabled(true);

		Log.Debug("Despawned plot {PlotId}", Id);
	}

	/// <summary>Wakes this peer's sleeping bodies that touch the bounds, as something they rest on may be leaving.</summary>
	internal void WakeAssemblies(Aabb bounds)
	{
		foreach (var assembly in _assemblies)
			if (assembly is { IsSimulated: true, IsKilled: false, Sleeping: true } &&
				(assembly.GlobalTransform * assembly.Bounds).Intersects(bounds))
				assembly.Sleeping = false;
	}

	// Jolt leaves bodies asleep when what they rest on leaves the space, so other creations' bodies leaning on this one
	// would hang in the air
	private void WakeNeighbors()
	{
		var bounds = _assemblies
			.Where(static assembly => !assembly.IsKilled)
			.Select(static assembly => assembly.GlobalTransform * assembly.Bounds)
			.Aggregate((Aabb?)null, static (sum, aabb) => sum?.Merge(aabb) ?? aabb);

		if (bounds is { } creationBounds)
			GPlotManager.WakeAssembliesNear(creationBounds.Grow(WakeMargin), this);
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

	private void KillFallenAssemblies(double delta)
	{
		_sinceKillCheck += delta;
		if (_sinceKillCheck < KillCheckInterval)
			return;

		_sinceKillCheck = 0;

		foreach (var assembly in _assemblies)
			if (assembly is { IsKilled: false, GlobalPosition.Y: < KillHeight })
			{
				assembly.Kill();
				Log.Debug("Killed fallen assembly {AssemblyId} of plot {PlotId}", assembly.Id, Id);
			}
	}
}
