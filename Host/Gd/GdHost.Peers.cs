namespace Root.Host.Gd;

public partial class GdHost
{
	public Godot.Collections.Dictionary<string, int> PeerIdsByPlayerId { get; } = [];

	// ReSharper disable once MemberCanBePrivate.Global
	public Godot.Collections.Dictionary<int, string> PlayerIdsByPeerId { get; } = [];

	private void AddPeer(int peerId, string playerId)
	{
		PlayerIdsByPeerId.Add(peerId, playerId);
		PeerIdsByPlayerId.Add(playerId, peerId);
	}

	private void RemovePeer(int peerId)
	{
		if (PlayerIdsByPeerId.Remove(peerId, out var playerId))
			PeerIdsByPlayerId.Remove(playerId);
	}

	private void ClearPeers()
	{
		PlayerIdsByPeerId.Clear();
		PeerIdsByPlayerId.Clear();
	}
}
