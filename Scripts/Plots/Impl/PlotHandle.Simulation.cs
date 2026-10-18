using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Sessions;
using Godot;
using Serilog;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	private const int HostPeerId = (int)MultiplayerPeer.TargetPeerServer;

	// The host takes over a creation whose simulator has sent nothing for this long (they send at least a heartbeat)
	private const double SimulatorTimeout = 3d;

	private int? _simulatorOverride;
	private double _sinceSimulatorHeard;

	/// <summary>
	///     Bumped whenever the creation's simulator may change (spawning, despawning, hand-offs), so streamed state
	///     stamped with another epoch is dropped rather than moving the wrong bodies. Peers bump it themselves on
	///     spawning and despawning, and take the host's on hand-offs.
	/// </summary>
	public ushort SimulationEpoch { get; private set; }

	/// <summary>
	///     The peer that simulates the creation; everyone else shows proxies of it. The host decides: the override if
	///     any, else the plot's owner, else itself.
	/// </summary>
	public int SimulatorPeerId { get; private set; } = HostPeerId;

	public bool IsSimulatedLocally => SimulatorPeerId == GSessionManager.LocalPeerId;

	/// <summary>
	///     Host only: makes a peer simulate the creation until the plot's owner changes or the plot despawns, or
	///     clears the override with null.
	/// </summary>
	public bool SetSimulatorOverride(int? peerId)
	{
		// A dedicated host has no peer of its own
		if (!GSessionManager.IsServer || (peerId is { } id and not HostPeerId && GSessionManager.GetPeer(id) is null))
			return false;

		_simulatorOverride = peerId;
		UpdateSimulator();

		return true;
	}

	private void ReadySimulation()
	{
		UpdateSimulator();

		_plot.Occupants.OwnerChanged += OnOwnerChanged;
		GSessionManager.PeerUnregistered += OnPeerUnregistered;
	}

	private void ExitSimulation()
	{
		_plot.Occupants.OwnerChanged -= OnOwnerChanged;
		GSessionManager.PeerUnregistered -= OnPeerUnregistered;
	}

	private void OnOwnerChanged(GdOccupant? _)
	{
		_simulatorOverride = null;
		UpdateSimulator();
	}

	private void OnPeerUnregistered(Peer peer)
	{
		if (_simulatorOverride == peer.Id)
			_simulatorOverride = null;

		UpdateSimulator();
	}

	private void ClearSimulatorOverride()
	{
		_simulatorOverride = null;
		UpdateSimulator();
	}

	// Host only, so every peer follows one decision, in the order it was made
	private void UpdateSimulator()
	{
		if (!GSessionManager.IsServer)
			return;

		var simulator = _simulatorOverride ?? GetOwnerPeerId() ?? HostPeerId;

		if (simulator == SimulatorPeerId)
			return;

		if (_plot.IsSpawned)
			SimulationEpoch++;

		SetSimulator(simulator);

		foreach (var peerId in GSessionManager.Peers.Keys)
			if (peerId != GSessionManager.LocalPeerId && GSessionManager.IsPeerSynced(peerId))
				RpcId(peerId, MethodName.RpcSetSimulator, simulator, SimulationEpoch);
	}

	private int? GetOwnerPeerId() =>
		_plot.Occupants.Owner is { } owner ? GSessionManager.GetPeerByPlayerId(owner.Player.Id)?.Id : null;

	[Rpc]
	private void RpcSetSimulator(int peerId, int epoch)
	{
		SimulationEpoch = (ushort)epoch;
		SetSimulator(peerId);
	}

	/// <summary>
	///     Hands the creation off: a new simulator resumes from the latest received state, and an old one freezes its
	///     bodies into proxies where they are. Remaining proxies forget their timeline, since another simulator's
	///     sequence numbers mean other moments.
	/// </summary>
	private void SetSimulator(int peerId)
	{
		SimulatorPeerId = peerId;
		_sinceSimulatorHeard = 0;

		if (!_plot.IsSpawned)
			return;

		var isSimulated = IsSimulatedLocally;

		// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
		foreach (var assembly in _assemblies)
			if (!assembly.IsKilled)
				assembly.HandOff(isSimulated, _clock);

		ResetTimeline();
		_awakeIds.Clear();
		_sinceStream = 0;

		Log.Debug(
			"Plot {PlotId}'s creation is simulated by peer {PeerId} (epoch {Epoch})", Id, peerId, SimulationEpoch);
	}

	// A simulator that stopped sending (a hung or hostile client) loses the creation to the host
	private void WatchSimulator(double delta)
	{
		if (!GSessionManager.IsServer || IsSimulatedLocally || !_plot.IsSpawned)
			return;

		_sinceSimulatorHeard += delta;
		if (_sinceSimulatorHeard < SimulatorTimeout)
			return;

		Log.Warning(
			"Peer {PeerId} sent nothing for plot {PlotId}'s creation in {Seconds} s; the host takes over",
			SimulatorPeerId, Id, SimulatorTimeout);

		_sinceSimulatorHeard = 0;
		SetSimulatorOverride(HostPeerId);
	}
}
