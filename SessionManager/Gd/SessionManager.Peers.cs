namespace Root.SessionManager.Gd;

// Basic helper for storing and adding/removing peers bound to players
public partial class SessionManager
{
	// Should probably expose these as IReadOnlyDictionary objects but Godot may not have that (?)
	public Godot.Collections.Dictionary<string, int> PeerIdsByPlayerId { get; } = [];
	public Godot.Collections.Dictionary<int, string> PlayerIdsByPeerId { get; } = [];

	private void AddPeer(int peerId, string playerId)
	{
		PlayerIdsByPeerId.Add(peerId, playerId);
		PeerIdsByPlayerId.Add(playerId, peerId);

		OnPeerConnected(peerId);
	}

	// ReSharper disable once MemberCanBeMadeStatic.Local
	private void RemovePeer(int peerId)
	{
		if (!PlayerIdsByPeerId.Remove(peerId, out var playerId))
			return;

		PeerIdsByPlayerId.Remove(playerId);
		OnPeerDisconnected(peerId);
	}

	private void ClearPeers()
	{
		foreach (var peerId in PeerIdsByPlayerId.Values)
			RemovePeer(peerId);
	}
}
