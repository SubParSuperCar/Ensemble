using System.Collections.ObjectModel;
using Root.Core.Gd.Player;

namespace Root.Ui.Impl.ViewModels;

public class PlayerListViewModel : ViewModelBase
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

	public override void Dispose()
	{
		GPlayers.Added -= OnPlayerAdded;
		GPlayers.Removed -= OnPlayerRemoved;

		GC.SuppressFinalize(this);
	}

	private void OnPlayerAdded(GdPlayer gdPlayer)
	{
		var player = new Player(
			gdPlayer.Name,
			GHost.PeerIdsByPlayerId.GetValueOrDefault(gdPlayer.Id, -1));

		Players.Add(player);
		_playersById[gdPlayer.Id] = player;
	}

	private void OnPlayerRemoved(GdPlayer gdPlayer)
	{
		if (_playersById.Remove(gdPlayer.Id, out var player))
			Players.Remove(player);
	}
}

public record Player(string Name, int PeerId);
