namespace Root.Host.Gd;

public partial class GdHost
{
	private readonly Dictionary<string, int> _peerIdsByPlayerId = [];
	private readonly Dictionary<int, string> _playerIdsByPeerId = [];

	public IReadOnlyDictionary<string, int> PeerIdsByPlayerId => _peerIdsByPlayerId;
	public IReadOnlyDictionary<int, string> PlayerIdsByPeerId => _playerIdsByPeerId;

	private void AddPeer(int peerId, string playerId)
	{
		_playerIdsByPeerId.Add(peerId, playerId);
		_peerIdsByPlayerId.Add(playerId, peerId);
	}

	private void RemovePeer(int peerId)
	{
		if (_playerIdsByPeerId.Remove(peerId, out var playerId))
			_peerIdsByPlayerId.Remove(playerId);
	}

	private void ClearPeers()
	{
		_playerIdsByPeerId.Clear();
		_peerIdsByPlayerId.Clear();
	}
}
