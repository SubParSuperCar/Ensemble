using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.GdCore.Players;
using EnsembleRoot.SessionManager.Api;
using EnsembleRoot.Ui.Impl.Abstractions;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class PlayerListViewModel : ViewModelBase
{
	private const int MaxPingMs = 999;

	private readonly Dictionary<string, PlayerItem> _playersById = [];
	private readonly Dictionary<string, Action> _unsubscribeByPlayerId = [];

	public PlayerListViewModel()
	{
		foreach (var player in GPlayers.GetAll())
			OnPlayerAdded(player);

		GPlayers.Added += OnPlayerAdded;
		GPlayers.Removed += OnPlayerRemoved;
	}

	public ObservableCollection<PlayerItem> Players { get; } = [];

	public bool IsMultiPlayer { get; } = GSessionManager.Mode is SessionMode.MultiPlayer;

	[ObservableProperty] public partial PlayerItem? SelectedPlayer { get; set; }

	protected override void OnDispose()
	{
		GPlayers.Added -= OnPlayerAdded;
		GPlayers.Removed -= OnPlayerRemoved;

		foreach (var unsubscribe in _unsubscribeByPlayerId.Values)
			unsubscribe();
	}

	private void OnPlayerAdded(GdPlayer gdPlayer)
	{
		var peer = GSessionManager.GetPeerByPlayerId(gdPlayer.Id);
		var player = new PlayerItem { Name = gdPlayer.Name, Id = gdPlayer.Id, PeerId = peer?.Id ?? None };

		var index = Players
			.TakeWhile(other => ComparePlayers(other, player) < 0)
			.Count();

		Players.Insert(index, player);
		_playersById.Add(gdPlayer.Id, player);

		if (ReferenceEquals(gdPlayer, GPlayers.Local))
			SelectedPlayer = player;

		if (peer is null)
			return;

		OnPingUpdated(peer.PingMs);
		peer.PingUpdated += OnPingUpdated;

		_unsubscribeByPlayerId.Add(gdPlayer.Id, Unsubscribe);

		return;

		void OnPingUpdated(int pingMs)
		{
			player.PingMs = Math.Min(pingMs, MaxPingMs);
		}

		void Unsubscribe()
		{
			peer.PingUpdated -= OnPingUpdated;
		}
	}

	private void OnPlayerRemoved(GdPlayer gdPlayer)
	{
		if (ReferenceEquals(gdPlayer, GPlayers.Local))
			SelectedPlayer = null;

		if (_playersById.Remove(gdPlayer.Id, out var player))
			Players.Remove(player);

		if (_unsubscribeByPlayerId.Remove(gdPlayer.Id, out var unsubscribe))
			unsubscribe();
	}

	private static int ComparePlayers(PlayerItem a, PlayerItem b)
	{
		var nameComparison = string.Compare(a.Name, b.Name, StringComparison.Ordinal);
		return nameComparison is 0 ? a.PeerId.CompareTo(b.PeerId) : nameComparison;
	}
}

public partial class PlayerItem : ObservableObject
{
	public required string Name { get; init; }
	public required string Id { get; init; }
	public int PeerId { get; init; }

	[ObservableProperty] public partial int PingMs { get; set; }
}
