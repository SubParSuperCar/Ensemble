using Godot;
using Godot.Collections;
using Serilog;
using GDictionary = Godot.Collections.Dictionary;

namespace EnsembleRoot.SessionManager;

public partial class SessionManager
{
	public const int MaxDisplayNameLength = 24;

	private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(1);

	private readonly System.Collections.Generic.Dictionary<int, Peer> _peersById = [];
	private readonly System.Collections.Generic.Dictionary<string, Peer> _peersByPlayerId = [];

	private double _sinceLastPingUpdate;

	public IReadOnlyDictionary<int, Peer> Peers => _peersById;
	public Peer? LocalPeer => GetPeer(LocalPeerId);

	/// <remarks>An empty display name is valid; Core then derives one from the player ID.</remarks>
	public static bool IsValidDisplayName(string? displayName) =>
		displayName is null ||
		(displayName.Length <= MaxDisplayNameLength && displayName.All(char.IsAsciiLetterOrDigit));

	public Peer? GetPeer(int peerId) => _peersById.GetValueOrDefault(peerId);
	public Peer? GetPeerByPlayerId(string playerId) => _peersByPlayerId.GetValueOrDefault(playerId);

	public Array<Peer> GetAllPeers() => [.. _peersById.Values];
	public Array<GDictionary> GetAllPeerDicts() => [.. _peersById.Values.Select(static peer => peer.ToDict())];

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

	[Rpc(TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void RpcUpdatePings(int[] peerIds, int[] pings)
	{
		if (peerIds.Length != pings.Length)
			return;

		for (var i = 0; i < peerIds.Length; i++)
			GetPeer(peerIds[i])?.SetPing(pings[i]);
	}

	private void RegisterPeer(int peerId, string displayName)
	{
		if (_peersById.ContainsKey(peerId))
			return;

		if (!IsValidDisplayName(displayName))
		{
			Log.Debug("Discarded invalid display name from peer {PeerId}: {DisplayName}", peerId, displayName);
			displayName = string.Empty;
		}

		foreach (var peer in _peersById.Values)
			RpcId(peerId, MethodName.RpcAddPeer, peer.Id, peer.PlayerId, peer.DisplayName);

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
		if (_peersById.ContainsKey(peerId) || _peersByPlayerId.ContainsKey(playerId))
			return;

		var peer = new Peer
		{
			Id = peerId,
			PlayerId = playerId,
			DisplayName = displayName,
			Address = _session?.GetAddress(peerId) ?? string.Empty,
			IsLocal = peerId == LocalPeerId
		};

		_peersById.Add(peerId, peer);
		_peersByPlayerId.Add(playerId, peer);

		Log.Debug("Registered {Peer}", peer);
		EmitSignal(SignalName.PeerRegistered, peer);

		if (peer.IsLocal)
			Activate();
	}

	private bool RemovePeer(int peerId)
	{
		if (!_peersById.Remove(peerId, out var peer))
			return false;

		_peersByPlayerId.Remove(peer.PlayerId);
		_syncedPeerIds.Remove(peerId);

		Log.Debug("Unregistered {Peer}", peer);
		EmitSignal(SignalName.PeerUnregistered, peer);

		return true;
	}

	private void ClearPeers()
	{
		foreach (var peerId in _peersById.Keys.ToArray())
			RemovePeer(peerId);
	}

	private void UpdatePings(double delta)
	{
		if (!IsServer || _session is not { } session || (_sinceLastPingUpdate += delta) < PingInterval.TotalSeconds)
			return;

		_sinceLastPingUpdate = 0;

		foreach (var peer in _peersById.Values.Where(static peer => !peer.IsHost))
			peer.SetPing(session.GetPing(peer.Id));

		RpcRegistered(
			MethodName.RpcUpdatePings,
			_peersById.Keys.ToArray(),
			_peersById.Values.Select(static peer => peer.PingMs).ToArray());
	}

	private static void OnPeerConnected(long peerId) => Log.Debug("Peer connected: {PeerId}", peerId);

	private void OnPeerDisconnected(long peerId)
	{
		Log.Debug("Peer disconnected: {PeerId}", peerId);

		DisposeRateLimiter((int)peerId);

		if (IsServer && RemovePeer((int)peerId))
			RpcRegistered(MethodName.RpcRemovePeer, (int)peerId);
	}
}
