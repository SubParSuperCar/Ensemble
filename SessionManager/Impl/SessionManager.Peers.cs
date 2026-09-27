using Godot;
using Serilog;
using GDictionary = Godot.Collections.Dictionary;
using PeerDicts = Godot.Collections.Dictionary<int, Godot.Collections.Dictionary>;

namespace EnsembleRoot.SessionManager;

public partial class SessionManager
{
	public const int MaxDisplayNameLength = 24;

	private readonly Dictionary<string, int> _peerIdsByPlayerId = [];
	private readonly Dictionary<int, PeerInfo> _peersById = [];

	public IReadOnlyDictionary<int, PeerInfo> Peers => _peersById;

	// Empty means "let Core generate one from the player ID"
	public static bool IsValidDisplayName(string? displayName) =>
		displayName is null ||
		(displayName.Length <= MaxDisplayNameLength && displayName.All(char.IsAsciiLetterOrDigit));

	public bool TryGetPeerId(string playerId, out int peerId) => _peerIdsByPlayerId.TryGetValue(playerId, out peerId);

	// The idea is that SessionManager be GDScript-friendly,
	// so expose a method for accessing critical peers without proprietary C# types
	public PeerDicts GetAllPeerDicts()
	{
		var result = new PeerDicts();

		foreach (var (peerId, info) in _peersById)
			result.Add(peerId, new GDictionary
			{
				["playerId"] = info.PlayerId,
				["displayName"] = info.DisplayName
			});

		return result;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcRequestRegister(string displayName)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		EnqueueRpc(senderId, 1, () => RegisterPeer(senderId, displayName));
	}

	[Rpc]
	private void RpcAddPeer(int peerId, string playerId, string displayName)
	{
		if (!Guid.TryParse(playerId, out var guid))
		{
			Log.Warning("Discarded malformed player id {PlayerId} for peer {PeerId}", playerId, peerId);
			return;
		}

		AddPeer(peerId, guid.ToString(), IsValidDisplayName(displayName) ? displayName : string.Empty);
	}

	[Rpc]
	private void RpcRemovePeer(int peerId) => RemovePeer(peerId);

	private void RegisterPeer(int peerId, string displayName)
	{
		if (_peersById.ContainsKey(peerId))
			return;

		if (!IsValidDisplayName(displayName))
		{
			Log.Debug("Discarded invalid display name from peer {PeerId}: {DisplayName}", peerId, displayName);
			displayName = string.Empty;
		}

		foreach (var (existingPeerId, info) in _peersById)
			RpcId(peerId, MethodName.RpcAddPeer, existingPeerId, info.PlayerId, info.DisplayName);

		var playerId = Guid.NewGuid().ToString();

		AddPeer(peerId, playerId, displayName);
		RpcRegistered(MethodName.RpcAddPeer, peerId, playerId, displayName);
	}

	private void RpcRegistered(StringName method, params Variant[] args)
	{
		foreach (var peerId in _peersById.Keys.Where(peerId => peerId != LocalPeerId))
			RpcId(peerId, method, args);
	}

	private void AddPeer(int peerId, string playerId, string displayName)
	{
		if (_peerIdsByPlayerId.ContainsKey(playerId) || !_peersById.TryAdd(peerId, new PeerInfo(playerId, displayName)))
			return;

		_peerIdsByPlayerId.Add(playerId, peerId);

		Log.Debug("Registered player {PlayerId} for peer {PeerId}", playerId, peerId);
		EmitSignal(SignalName.PlayerRegistered, peerId, playerId, displayName);

		if (peerId == LocalPeerId)
			Activate();
	}

	private bool RemovePeer(int peerId)
	{
		if (!_peersById.Remove(peerId, out var info))
			return false;

		_peerIdsByPlayerId.Remove(info.PlayerId);
		_syncedPeerIds.Remove(peerId);

		Log.Debug("Unregistered player {PlayerId} for peer {PeerId}", info.PlayerId, peerId);
		EmitSignal(SignalName.PlayerUnregistered, peerId, info.PlayerId);

		return true;
	}

	private void ClearPeers()
	{
		foreach (var peerId in _peersById.Keys.ToArray())
			RemovePeer(peerId);
	}

	private static void OnPeerConnected(long peerId) => Log.Debug("Peer connected: {PeerId}", peerId);

	private void OnPeerDisconnected(long peerId)
	{
		Log.Debug("Peer disconnected: {PeerId}", peerId);

		DisposeRateLimiter((int)peerId);

		if (IsServer && RemovePeer((int)peerId))
			RpcRegistered(MethodName.RpcRemovePeer, (int)peerId);
	}

	public readonly record struct PeerInfo(string PlayerId, string DisplayName);
}
