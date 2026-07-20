namespace Root.SessionManager.Gd;

public partial class SessionManager
{
	public Godot.Collections.Dictionary<string, int> PeerIdsByPlayerId { get; } = [];

	// ReSharper disable once MemberCanBePrivate.Global
	public Godot.Collections.Dictionary<int, string> PlayerIdsByPeerId { get; } = [];

	private void AddPeer(int peerId, string playerId)
	{
		PlayerIdsByPeerId.Add(peerId, playerId);
		PeerIdsByPlayerId.Add(playerId, peerId);

		OnPeerConnected(peerId);
	}

	private void RemovePeer(int peerId)
	{
		if (!PlayerIdsByPeerId.Remove(peerId, out var playerId))
			return;

		PeerIdsByPlayerId.Remove(playerId);
		OnPeerDisconnected(peerId);
	}

	private void ClearPeers()
	{
		foreach (var peerId in PeerIdsByPlayerId.Values.ToArray())
			RemovePeer(peerId);
	}
}
