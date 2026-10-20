using EnsembleRoot.Scripts.Plots;
using Godot;
using Serilog;

namespace EnsembleRoot.Scripts.Creations;

/// <summary>
///     A plot's creation while it's spawned: assemblies built from the plot's instances, alike on every peer, which this
///     peer either simulates and streams or shows as proxies of the simulator's stream. It's created on spawning and
///     freed on despawning, so nothing of it outlives a spawn. Its plot decides who simulates it (see
///     <see cref="PlotHandle.SimulatorPeerId" />) and routes its packets.
/// </summary>
public partial class LiveCreation : Node3D
{
	private const float KillHeight = -1024f;
	private const double KillCheckInterval = 1d;

	private readonly List<Assembly> _assemblies = [];

	private double _sinceKillCheck;

	public PlotHandle Plot { get; init; } = null!;

	/// <summary>The plot's instance origin, to which built and streamed transforms are relative.</summary>
	public Transform3D Origin { get; init; }

	/// <summary>The assemblies, each at the index that is its ID.</summary>
	public IReadOnlyList<Assembly> Assemblies => _assemblies;

	public override void _PhysicsProcess(double delta)
	{
		UpdateStream(delta);
		KillFallenAssemblies(delta);
	}

	/// <summary>
	///     Hands the creation to this peer or away from it: a new simulator resumes from the latest received states, and
	///     an old one freezes its bodies into proxies where they are. The stream and the proxies' timelines start over,
	///     since another simulator's sequence numbers mean other moments.
	/// </summary>
	internal void HandOff(bool isSimulated)
	{
		// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
		foreach (var assembly in _assemblies)
			if (!assembly.IsKilled)
				assembly.HandOff(isSimulated, _clock);

		ResetStream();
	}

	// Only despawning frees a creation, so assemblies that fell out of the world are switched off instead
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
				Log.Debug("Killed fallen assembly {AssemblyId} of plot {PlotId}", assembly.Id, Plot.Id);
			}
	}
}
