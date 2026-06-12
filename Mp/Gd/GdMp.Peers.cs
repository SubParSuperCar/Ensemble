namespace Root.Mp.Gd;

public partial class GdMp
{
	// ReSharper disable once CollectionNeverQueried.Local
	private readonly Dictionary<int, string> _peers = [];

	private void AddPeer(int peerId, string playerId) => _peers.Add(peerId, playerId);
	private void RemovePeer(int peerId) => _peers.Remove(peerId);

	private void ClearPeers() => _peers.Clear();
}
