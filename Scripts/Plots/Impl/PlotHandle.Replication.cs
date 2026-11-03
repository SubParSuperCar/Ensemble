using EnsembleRoot.Sessions;
using EnsembleRoot.Sessions.Api;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	// Only peers whose budget doesn't reach this creation's full rate have an entry; see PlotManager.UpdateRelayBudgets
	private readonly System.Collections.Generic.Dictionary<int, int> _relayDivisorsByPeerId = [];

	private long _relayedBytes;

	internal static bool IsStreaming => GSessionManager is { Mode: SessionMode.MultiPlayer, IsActive: true };

	/// <summary>
	///     The simulation epoch, the simulator, and every assembly's latest state (none unless spawned), for a joining
	///     peer.
	/// </summary>
	internal (ushort Epoch, int SimulatorPeerId, byte[] States, Dictionary Seats) CaptureCreation() =>
		(SimulationEpoch, SimulatorPeerId, Creation?.CaptureStates() ?? [], Creation?.CaptureSeats() ?? []);

	/// <summary>Adopts a joining snapshot's simulation state, once the snapshot has spawned the plot.</summary>
	internal void RestoreCreation(ushort epoch, int simulatorPeerId, byte[] states)
	{
		SimulationEpoch = epoch;
		SetSimulator(simulatorPeerId);

		if (!IsSimulatedLocally)
			Creation?.RestoreStates(states);
	}

	/// <summary>
	///     Seats a joining snapshot's riders, once every plot has its occupants, as changing plots leaves seats.
	/// </summary>
	internal void RestoreSeats(Dictionary seats) => Creation?.RestoreSeats(seats);

	/// <summary>Sends a packet of the creation's states on, from its simulator.</summary>
	internal void SendCreation(byte[] data, ushort sequence) => Send(data, sequence, GSessionManager.LocalPeerId);

	/// <summary>The bytes per second the creation's stream has run at, for the host, since this was last asked.</summary>
	internal double TakeRelayRate(double elapsed)
	{
		var rate = _relayedBytes / elapsed;
		_relayedBytes = 0;

		return rate;
	}

	/// <summary>Has the host relay only every <paramref name="divisor" />th stream of the creation to a peer.</summary>
	internal void SetRelayDivisor(int peerId, int divisor)
	{
		if (divisor > 1)
			_relayDivisorsByPeerId[peerId] = divisor;
		else
			_relayDivisorsByPeerId.Remove(peerId);
	}

	// The host relays to every other synced peer, as often as its budget allows; anyone else sends to the host. A
	// stream's packets share a sequence, so a peer gets them all or none, and divisors are powers of two, so the
	// sequence's wrap keeps the rhythm
	private void Send(byte[] data, ushort sequence, int sourcePeerId)
	{
		if (!GSessionManager.IsServer)
		{
			RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcReceiveCreation, data);
			return;
		}

		_relayedBytes += data.Length;

		foreach (var peerId in GSessionManager.Peers.Keys)
			if (
				peerId != sourcePeerId && GSessionManager.IsPeerSynced(peerId) &&
				sequence % _relayDivisorsByPeerId.GetValueOrDefault(peerId, 1) is 0)
				RpcId(peerId, MethodName.RpcReceiveCreation, data);
	}

	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered,
		TransferChannel = NetworkChannels.Creations)]
	private void RpcReceiveCreation(byte[] data)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		NetworkConditions.Receive(this, () => ReceiveCreation(senderId, data));
	}

	// The host takes only the simulator's packets and relays those its creation accepts; everyone else takes only the
	// host's relays
	private void ReceiveCreation(int senderId, byte[] data)
	{
		var isServer = GSessionManager.IsServer;

		if (senderId != (isServer ? SimulatorPeerId : HostPeerId) || IsSimulatedLocally || Creation is not { } creation)
			return;

		if (!creation.TryReceive(data, isServer, out var sequence) || !isServer)
			return;

		_sinceSimulatorHeard = 0;
		Send(data, sequence, senderId);
	}
}
