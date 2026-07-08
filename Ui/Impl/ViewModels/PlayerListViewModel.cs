using System.Collections.ObjectModel;
using Root.Ui.Impl.Abstractions;

namespace Root.Ui.Impl.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public class PlayerListViewModel : ViewModelBase
{
	private readonly Dictionary<int, Player> _playersByPeerId = [];

	public PlayerListViewModel()
	{
		foreach (var peerId in GSessionManager.PeerIdsByPlayerId.Values)
			OnPeerConnected(peerId);

		GSessionManager.PeerConnected += OnPeerConnected;
		GSessionManager.PeerDisconnected += OnPeerDisconnected;
	}

	public ObservableCollection<Player> Players { get; } = [];

	protected override void OnDispose()
	{
		GSessionManager.PeerConnected -= OnPeerConnected;
		GSessionManager.PeerDisconnected -= OnPeerDisconnected;
	}

	private void OnPeerConnected(int peerId)
	{
		var playerId = GSessionManager.PlayerIdsByPeerId[peerId];

		var player = new Player(
			GPlayers.Get(playerId)!.Name,
			GSessionManager.PeerIdsByPlayerId.GetValueOrDefault(playerId, -1));

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
