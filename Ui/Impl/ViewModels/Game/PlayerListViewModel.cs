using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Root.GdCore.Players;
using Root.Ui.Impl.Abstractions;

namespace Root.Ui.Impl.ViewModels;

public partial class PlayerListViewModel : ViewModelBase
{
	private readonly Dictionary<string, Player> _playersById = [];

	public PlayerListViewModel()
	{
		foreach (var player in GPlayers.GetAll())
			OnPlayerAdded(player);

		GPlayers.Added += OnPlayerAdded;
		GPlayers.Removed += OnPlayerRemoved;
	}

	public ObservableCollection<Player> Players { get; } = [];

	[ObservableProperty] public partial Player? SelectedPlayer { get; set; }

	protected override void OnDispose()
	{
		GPlayers.Added -= OnPlayerAdded;
		GPlayers.Removed -= OnPlayerRemoved;
	}

	private void OnPlayerAdded(GdPlayer gdPlayer)
	{
		int? peerId = null;
		foreach (var (id, peer) in GSessionManager.Peers)
			if (string.Equals(peer.PlayerId, gdPlayer.Id, StringComparison.Ordinal))
			{
				peerId = id;
				break;
			}

		var player = new Player(gdPlayer.Name, gdPlayer.Id, peerId ?? None);

		var index = Players
			.TakeWhile(p => string.Compare(p.Name, player.Name, StringComparison.Ordinal) < 0)
			.Count();

		Players.Insert(index, player);
		_playersById[gdPlayer.Id] = player;

		if (ReferenceEquals(gdPlayer, GPlayers.Local))
			SelectedPlayer = player;
	}

	private void OnPlayerRemoved(GdPlayer gdPlayer)
	{
		if (ReferenceEquals(gdPlayer, GPlayers.Local))
			SelectedPlayer = null;

		if (_playersById.Remove(gdPlayer.Id, out var player))
			Players.Remove(player);
	}
}

public record Player(string Name, string Id, int PeerId);
