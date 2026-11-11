using EnsembleRoot.Common.Input;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Plots;
using Godot;
using Serilog;

namespace EnsembleRoot.Scripts.Creations;

/// <summary>
///     A plot's creation while it's spawned: assemblies built from the plot's instances, alike on every peer, which
///     this peer either simulates and streams or shows as proxies of the simulator's stream. It's created on spawning
///     and freed on despawning, so nothing of it outlives a spawn. Its plot decides who simulates it (see
///     <see cref="PlotHandle.SimulatorPeerId" />) and routes its packets.
/// </summary>
public partial class LiveCreation : Node3D
{
	private const float KillHeight = -1024f;

	private readonly List<Assembly> _assemblies = [];
	private readonly List<(AssetHandle Handle, ISimulationTicked Ticked)> _ticked = [];

	private bool _isAwaitingInput;
	private bool _isResuming;
	private InputState _previousInput;

	public PlotHandle Plot { get; init; } = null!;

	/// <summary>The plot's instance origin, to which built and streamed transforms are relative.</summary>
	public Transform3D Origin { get; init; }

	/// <summary>The assemblies, each at the index that is its ID.</summary>
	public IReadOnlyList<Assembly> Assemblies => _assemblies;

	internal InputState Input
	{
		get;
		set
		{
			// Keys already held when a simulator takes over aren't new presses
			if (_isAwaitingInput)
				_previousInput = value;

			_isAwaitingInput = false;
			field = value;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		KillLostAssemblies();
		Tick(delta);
		UpdateStream(delta);
	}

	/// <summary>
	///     Hands the creation to this peer or away from it: a new simulator resumes from the latest received states,
	///     and an old one freezes its bodies into proxies where they are. The stream and the proxies' timelines start
	///     over, since another simulator's sequence numbers mean other moments.
	/// </summary>
	internal void HandOff(bool isSimulated)
	{
		// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
		foreach (var assembly in _assemblies)
			if (!assembly.IsKilled)
				assembly.HandOff(isSimulated, _clock);

		// The controller's next update reaches whoever simulates now
		Input = default;
		_isAwaitingInput = true;
		_isResuming = true;

		ForgetSentAssetStates();
		ResetStream();
	}

	private void Tick(double delta)
	{
		if (!Plot.IsSimulatedLocally)
			return;

		var tick = new SimulationTick(Input, _previousInput, delta, _isResuming);

		foreach (var (handle, ticked) in _ticked)
			if (handle.Assembly is { IsKilled: false })
				ticked.Tick(tick);

		_previousInput = Input;
		_isResuming = false;
	}

	// Every tick, before anything reads or draws them: a frame drawn with a blown-up body stalls rendering for seconds
	private void KillLostAssemblies()
	{
		foreach (var assembly in _assemblies)
		{
			if (assembly.IsKilled)
				continue;

			var transform = assembly.IsSimulated ? assembly.GlobalTransform : assembly.ProxyTransform;

			if (transform.Origin.Y < KillHeight || !transform.IsFinite())
				Kill(assembly);
		}
	}

	// A blown-up body is put below the kill height first, so the state it's kept alive with takes its proxies along
	private void Kill(Assembly assembly)
	{
		var isBlownUp = !assembly.GlobalTransform.IsFinite();

		if (isBlownUp)
			assembly.GlobalTransform = new Transform3D(Basis.Identity, Origin.Origin with { Y = 2 * KillHeight });

		assembly.Kill();
		VacateSeats(assembly);

		Log.Debug(
			"Killed {Reason} assembly {AssemblyId} of plot {PlotId}", isBlownUp ? "blown-up" : "fallen", assembly.Id,
			Plot.Id);
	}
}
