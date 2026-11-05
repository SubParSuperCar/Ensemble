using EnsembleRoot.Sessions;
using EnsembleRoot.Sessions.Api;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	// Only peers who don't get the full rate have an entry
	private readonly System.Collections.Generic.Dictionary<int, int> _relayDivisorsByPeerId = [];

	private long _relayedBytes;

	internal static bool IsStreaming => GSessionManager is { Mode: SessionMode.MultiPlayer, IsActive: true };

	internal (ushort Epoch, int SimulatorPeerId, byte[] States, int[] AssetStates, Dictionary Seats)
		CaptureCreation() =>
		(SimulationEpoch, SimulatorPeerId, Creation?.CaptureStates() ?? [], Creation?.CaptureAssetStates() ?? [],
			Creation?.CaptureSeats() ?? []);

	/// <summary>Adopts a joining snapshot's simulation state, once the snapshot has spawned the plot.</summary>
	internal void RestoreCreation(ushort epoch, int simulatorPeerId, byte[] states)
	{
		SimulationEpoch = epoch;
		SetSimulator(simulatorPeerId);

		if (!IsSimulatedLocally)
			Creation?.RestoreStates(states);
	}

	internal void RestoreAssetStates(int[] states) => Creation?.TryReceiveAssetStates(states, false);

	/// <summary>
	///     Seats a joining snapshot's riders, once every plot has its occupants, as changing plots leaves seats.
	/// </summary>
	internal void RestoreSeats(Dictionary seats) => Creation?.RestoreSeats(seats);

	internal void SendCreation(byte[] data, ushort sequence) => Send(data, sequence, GSessionManager.LocalPeerId);

	internal void SendAssetStates(int[] states)
	{
		if (GSessionManager.IsServer)
			RelayAssetStates(states, GSessionManager.LocalPeerId);
		else
			RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcReceiveAssetStates, SimulationEpoch, states);
	}

	internal double TakeRelayRate(double elapsed)
	{
		var rate = _relayedBytes / elapsed;
		_relayedBytes = 0;

		return rate;
	}

	internal void SetRelayDivisor(int peerId, int divisor)
	{
		if (divisor > 1)
			_relayDivisorsByPeerId[peerId] = divisor;
		else
			_relayDivisorsByPeerId.Remove(peerId);
	}

	// A stream's packets share a sequence, so a peer gets them all or none
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
				RelayBudget.IsRelayed(sequence, _relayDivisorsByPeerId.GetValueOrDefault(peerId, 1)))
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

	private void ReceiveCreation(int senderId, byte[] data)
	{
		var isServer = GSessionManager.IsServer;

		if (!IsFromSimulator(senderId) || Creation is not { } creation)
			return;

		if (!creation.TryReceive(data, isServer, out var sequence) || !isServer)
			return;

		_sinceSimulatorHeard = 0;
		Send(data, sequence, senderId);
	}

	// Reliable, on the channel actions and hand-offs share, so they stay in order with spawning and simulator changes
	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcReceiveAssetStates(int epoch, int[] states)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		var isServer = GSessionManager.IsServer;

		if (
			!IsFromSimulator(senderId) || epoch != SimulationEpoch || Creation is not { } creation ||
			!creation.TryReceiveAssetStates(states, isServer) || !isServer)
			return;

		RelayAssetStates(states, senderId);
	}

	private void RelayAssetStates(int[] states, int sourcePeerId)
	{
		foreach (var peerId in GSessionManager.Peers.Keys)
			if (peerId != sourcePeerId && GSessionManager.IsPeerSynced(peerId))
				RpcId(peerId, MethodName.RpcReceiveAssetStates, SimulationEpoch, states);
	}

	// The host takes only its simulator's, and everyone else only the host's
	private bool IsFromSimulator(int senderId) =>
		!IsSimulatedLocally &&
		(GSessionManager.IsServer
			? senderId == SimulatorPeerId && GSessionManager.IsPeerSynced(senderId)
			: senderId == HostPeerId);
}
