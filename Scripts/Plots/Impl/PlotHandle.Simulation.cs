using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Sessions;
using Godot;
using Serilog;

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
	///     any, else the arbitrated simulator, else the plot's owner, else itself.
	/// </summary>
	public int SimulatorPeerId { get; private set; } = HostPeerId;

	public bool IsSimulatedLocally => SimulatorPeerId == GSessionManager.LocalPeerId;

	/// <summary>
	///     Host only: makes a peer simulate the creation until the plot's owner changes or the plot despawns, or
	///     clears the override with null.
	/// </summary>
	public bool SetSimulatorOverride(int? peerId)
	{
		// The host is always valid, though a dedicated one has no registered peer of its own; a peer still syncing
		// would simulate from the spawn pose, as its snapshot doesn't move the bodies it simulates
		if (!GSessionManager.IsServer || (peerId is { } id and not HostPeerId && !GSessionManager.IsPeerSynced(id)))
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

	private void OnOwnerChanged(GdOccupant? _) => ClearSimulatorOverride();

	// A departing owner arrives as an owner change, which may come before or after this, so only the peers an override
	// or arbitration chose matter here
	private void OnPeerUnregistered(Peer peer)
	{
		if (_simulatorOverride == peer.Id)
			_simulatorOverride = null;

		if (ArbitratedSimulatorPeerId == peer.Id)
			ArbitratedSimulatorPeerId = null;

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

		var simulator = _simulatorOverride ?? ArbitratedSimulatorPeerId ?? GetOwnerPeerId() ?? HostPeerId;

		if (simulator == SimulatorPeerId)
			return;

		if (_plot.IsSpawned)
			SimulationEpoch++;

		SetSimulator(simulator);

		foreach (var peerId in GSessionManager.Peers.Keys)
			if (IsSyncedRemote(peerId))
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

	// A spawned creation is handed off at once (see LiveCreation.HandOff); an unspawned one starts out simulated by
	// the peer
	private void SetSimulator(int peerId)
	{
		SimulatorPeerId = peerId;
		_sinceSimulatorHeard = 0;

		if (Creation is null)
			return;

		Creation.HandOff(IsSimulatedLocally);

		Log.Debug(
			"Plot {PlotId}'s creation is simulated by peer {PeerId} (epoch {Epoch})", Id, peerId, SimulationEpoch);
	}

	// A simulator that stopped sending (a hung or hostile client) loses the creation to the host
	private void WatchSimulator(double delta)
	{
		if (!GSessionManager.IsServer || IsSimulatedLocally || Creation is null)
			return;

		_sinceSimulatorHeard += delta;
		if (_sinceSimulatorHeard < SimulatorTimeout)
			return;

		Log.Warning(
			"Plot {PlotId}'s simulator, peer {PeerId}, was silent for {Seconds} s; the host takes over",
			Id, SimulatorPeerId, SimulatorTimeout);

		_sinceSimulatorHeard = 0;
		SetSimulatorOverride(HostPeerId);
	}
}
