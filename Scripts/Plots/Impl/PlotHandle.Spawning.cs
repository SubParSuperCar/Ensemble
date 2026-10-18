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

	private const uint AssemblyMask =
		PhysicsLayers.World | PhysicsLayers.Character | PhysicsLayers.StaticInstances | PhysicsLayers.DynamicInstances;

	private readonly List<Assembly> _assemblies = [];

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
