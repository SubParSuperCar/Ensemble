namespace Root.Host.Gd;

public partial class GdHost
{
	// ReSharper disable once CollectionNeverQueried.Local
	private readonly Dictionary<int, string> _playerIdsByPeerId = [];

	private void AddPeer(int peerId, string playerId) => _playerIdsByPeerId.Add(peerId, playerId);
	private void RemovePeer(int peerId) => _playerIdsByPeerId.Remove(peerId);

	private void ClearPeers() => _playerIdsByPeerId.Clear();
}
