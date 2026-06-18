namespace Root.Session.Gd;

public partial class GdSession
{
	// ReSharper disable once CollectionNeverQueried.Local
	private readonly Dictionary<int, string> _peersById = [];

	private void AddPeer(int peerId, string playerId) => _peersById.Add(peerId, playerId);
	private void RemovePeer(int peerId) => _peersById.Remove(peerId);

	private void ClearPeers() => _peersById.Clear();
}
