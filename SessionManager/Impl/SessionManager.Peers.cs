using Godot;
using Serilog;

// ReSharper disable UnusedMember.Global

namespace Root.SessionManager;

public partial class SessionManager
{
	private readonly Dictionary<string, int> _peerIdsByPlayerId = [];
	private readonly Dictionary<int, PeerInfo> _peersById = [];

	public IReadOnlyDictionary<int, PeerInfo> Peers => _peersById;

	public bool TryGetPeerId(string playerId, out int peerId) => _peerIdsByPlayerId.TryGetValue(playerId, out peerId);

	private void RegisterLocalPlayer(string playerId, string displayName)
	{
		if (IsServer)
			ConfirmRegistration(LocalPeerId, playerId, displayName);
		else
			RpcId(1, MethodName.RpcRequestRegister, playerId, displayName);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcRequestRegister(string playerId, string displayName)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		EnqueueRpc(senderId, 1, () => ConfirmRegistration(senderId, playerId, displayName));
	}

	private void ConfirmRegistration(int peerId, string playerId, string displayName)
	{
		if (_peersById.ContainsKey(peerId))
			return;

		foreach (var (existingPeerId, info) in _peersById)
			RpcId(peerId, MethodName.RpcConfirmRegister, existingPeerId, info.PlayerId, info.DisplayName);

		AddPeer(peerId, playerId, displayName);
		Rpc(MethodName.RpcConfirmRegister, peerId, playerId, displayName);
	}

	[Rpc(CallLocal = false)]
	private void RpcConfirmRegister(int peerId, string playerId, string displayName) =>
		AddPeer(peerId, playerId, displayName);

	private void BroadcastUnregister(int peerId)
	{
		if (RemovePeer(peerId))
			Rpc(MethodName.RpcConfirmUnregister, peerId);
	}

	[Rpc(CallLocal = false)]
	private void RpcConfirmUnregister(int peerId) => RemovePeer(peerId);

	private void AddPeer(int peerId, string playerId, string displayName)
	{
		if (!_peersById.TryAdd(peerId, new PeerInfo(playerId, displayName)))
			return;

		_peerIdsByPlayerId[playerId] = peerId;

		Log.Debug("Registered player {PlayerId} for peer {PeerId}.", playerId, peerId);
		EmitSignal(SignalName.PlayerRegistered, peerId, playerId, displayName);
	}

	private bool RemovePeer(int peerId)
	{
		if (!_peersById.Remove(peerId, out var info))
			return false;

		_peerIdsByPlayerId.Remove(info.PlayerId);

		Log.Debug("Unregistered player {PlayerId} for peer {PeerId}.", info.PlayerId, peerId);
		EmitSignal(SignalName.PlayerUnregistered, peerId, info.PlayerId);

		return true;
	}

	private void ClearPeers()
	{
		foreach (var peerId in _peersById.Keys.ToArray())
			RemovePeer(peerId);
	}

	public readonly record struct PeerInfo(string PlayerId, string DisplayName);
}
