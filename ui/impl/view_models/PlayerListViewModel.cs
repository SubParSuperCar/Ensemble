using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Root.Core.Gd.Player;
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
		var peerId = GSessionManager.PeerIdsByPlayerId.GetValueOrDefault(gdPlayer.Id, None);

		var player = new Player(gdPlayer.Name, peerId);
		Players.Add(player);
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

public record Player(string Name, int PeerId);
