using System.Diagnostics;
using System.Globalization;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Scripts.Assets;
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

	private const uint AssemblyMask =
		PhysicsLayers.World | PhysicsLayers.Character | PhysicsLayers.StaticInstances | PhysicsLayers.DynamicInstances;

	private readonly List<Assembly> _assemblies = [];
	private readonly List<Obb> _liftCandidates = [];
	private readonly PlacementSolver _solver = new();

	private Node3D _dynamicInstances = null!;
	private double _sinceKillCheck;

	public IReadOnlyList<Assembly> Assemblies => _assemblies;

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

	// Every peer builds the same assemblies from the plot's state, so spawning sends no geometry
	private void Spawn()
	{
		var timestamp = Stopwatch.GetTimestamp();
		var instances = _plot.Instances.GetAll().ToDictionary(static instance => instance.Id);
		var parts = instances.Values.Select(static instance => (instance.Id, GetColliderBoxes(instance))).ToList();

		SetStaticInstancesEnabled(false);

		foreach (var group in Welds.Group(parts))
			AddAssembly(group, instances);

		foreach (var assembly in _assemblies)
			foreach (var member in assembly.Members)
				member.Initialize(this, instances[member.InstanceId]);

		MoveLocalCharacterClear();

		_sinceKillCheck = 0;
		SetPhysicsProcess(true);

		Log.Debug(
			"Spawned plot {PlotId} as {AssemblyCount} assemblies in {ElapsedMs:F3} ms", Id, _assemblies.Count,
			Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);
	}

	private void Despawn()
	{
		SetPhysicsProcess(false);

		foreach (var assembly in _assemblies)
			assembly.QueueFree();

		_assemblies.Clear();
		SetStaticInstancesEnabled(true);

		Log.Debug("Despawned plot {PlotId}", Id);
	}

	private void SetStaticInstancesEnabled(bool isEnabled)
	{
		_staticInstances.ProcessMode = isEnabled ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
		_staticInstances.Visible = isEnabled;
	}

	private static Obb[] GetColliderBoxes(GdInstance instance)
	{
		var transform = GetTransform(instance);
		return [.. GAssetManager.GetColliderBounds(instance.Asset.Id).Select(bounds => Obb.From(bounds, transform))];
	}

	private void AddAssembly(int[] group, Dictionary<int, GdInstance> instances)
	{
		var assembly = new Assembly
		{
			Name = group[0].ToString(CultureInfo.InvariantCulture),
			Id = group[0],
			Transform = _staticInstances.GlobalTransform,
			Freeze = true,
			CollisionLayer = PhysicsLayers.DynamicInstances,
			CollisionMask = AssemblyMask
		};

		_dynamicInstances.AddChild(assembly);

		foreach (var id in group)
			assembly.AddMember(CreateHandle(instances[id], GetTransform(instances[id])));

		assembly.Release();
		assembly.ResetPhysicsInterpolation();
		_assemblies.Add(assembly);
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
