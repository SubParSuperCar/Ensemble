using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.GdCore.Players;
using EnsembleRoot.Ui.Impl.Abstractions;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class PlayerListViewModel : ViewModelBase
{
	private readonly Dictionary<string, PlayerItem> _playersById = [];

	public PlayerListViewModel()
	{
		foreach (var player in GPlayers.GetAll())
			OnPlayerAdded(player);

		GPlayers.Added += OnPlayerAdded;
		GPlayers.Removed += OnPlayerRemoved;
	}

	public ObservableCollection<PlayerItem> Players { get; } = [];

	[ObservableProperty] public partial PlayerItem? SelectedPlayer { get; set; }

	protected override void OnDispose()
	{
		GPlayers.Added -= OnPlayerAdded;
		GPlayers.Removed -= OnPlayerRemoved;
	}

	private void OnPlayerAdded(GdPlayer gdPlayer)
	{
		var peerId = GSessionManager.GetPeerByPlayerId(gdPlayer.Id)?.Id ?? None;
		var player = new PlayerItem(gdPlayer.Name, gdPlayer.Id, peerId);

		var index = Players
			.TakeWhile(other => string.Compare(other.Name, player.Name, StringComparison.Ordinal) < 0)
			.Count();

		Players.Insert(index, player);
		_playersById.Add(gdPlayer.Id, player);

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

public record PlayerItem(string Name, string Id, int PeerId);
