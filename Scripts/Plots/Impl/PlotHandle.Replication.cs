using EnsembleRoot.Sessions;
using EnsembleRoot.Sessions.Api;
using Godot;

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	private const int CreationChannel = 2;

	internal static bool IsStreaming => GSessionManager is { Mode: SessionMode.MultiPlayer, IsActive: true };

	private static bool IsSyncedRemote(int peerId) =>
		peerId != GSessionManager.LocalPeerId && GSessionManager.IsPeerSynced(peerId);

	/// <summary>
	///     The simulation epoch, the simulator, and every assembly's latest state (none unless spawned), for a joining
	///     peer.
	/// </summary>
	public (ushort Epoch, int SimulatorPeerId, byte[] States) CaptureCreation() =>
		(SimulationEpoch, SimulatorPeerId, Creation?.CaptureStates() ?? []);

	/// <summary>Adopts a joining snapshot's simulation state, once the snapshot has spawned the plot.</summary>
	public void RestoreCreation(ushort epoch, int simulatorPeerId, byte[] states)
	{
		SimulationEpoch = epoch;
		SetSimulator(simulatorPeerId);

		if (!IsSimulatedLocally)
			Creation?.RestoreStates(states);
	}

	/// <summary>Sends a packet of the creation's states on, from its simulator.</summary>
	internal void SendCreation(byte[] data) => Send(data, GSessionManager.LocalPeerId);

	// The host relays to every other synced peer; anyone else sends to the host
	private void Send(byte[] data, int sourcePeerId)
	{
		if (!GSessionManager.IsServer)
		{
			RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcReceiveCreation, data);
			return;
		}

		foreach (var peerId in GSessionManager.Peers.Keys)
			if (peerId != sourcePeerId && IsSyncedRemote(peerId))
				RpcId(peerId, MethodName.RpcReceiveCreation, data);
	}

	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered,
		TransferChannel = CreationChannel)]
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

		if (senderId != (isServer ? SimulatorPeerId : HostPeerId) || IsSimulatedLocally ||
			Creation is not { } creation || !creation.TryReceive(data, isServer) || !isServer)
			return;

		_sinceSimulatorHeard = 0;
		Send(data, senderId);
	}
}
