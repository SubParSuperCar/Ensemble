using System.Collections.ObjectModel;

namespace Root.Ui.Impl.ViewModels;

public class PlayerListViewModel : ViewModelBase
{
	private readonly Dictionary<int, Player> _playersByPeerId = [];

	public PlayerListViewModel()
	{
		foreach (var peerId in GHost.PeerIdsByPlayerId.Values)
			OnPeerConnected(peerId);

		GHost.PeerConnected += OnPeerConnected;
		GHost.PeerDisconnected += OnPeerDisconnected;
	}

	public ObservableCollection<Player> Players { get; } = [];

	protected override void OnDispose()
	{
		GHost.PeerConnected -= OnPeerConnected;
		GHost.PeerDisconnected -= OnPeerDisconnected;
	}

	private void OnPeerConnected(int peerId)
	{
		var playerId = GHost.PlayerIdsByPeerId[peerId];

		var player = new Player(
			GPlayers.Get(playerId)!.Name,
			GHost.PeerIdsByPlayerId.GetValueOrDefault(playerId, -1));

		Players.Add(player);
		_playersByPeerId[peerId] = player;
	}

	private void OnPeerDisconnected(int peerId)
	{
		if (_playersByPeerId.Remove(peerId, out var player))
			Players.Remove(player);
	}
}

public record Player(string Name, int PeerId);
