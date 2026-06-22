using System.Collections.ObjectModel;
using Root.Core.Gd.Player;
using Root.Gd.Globals;

namespace Root.Ui.Impl.ViewModels;

// ReSharper disable once UnusedType.Global
public class PlayerListViewModel : ViewModelBase, IDisposable
{
	private readonly Dictionary<string, Player> _playersById = [];

	public PlayerListViewModel()
	{
		foreach (var player in GdGlobals.Players.GetAll())
			OnPlayerAdded(player);

		GdGlobals.Players.Added += OnPlayerAdded;
		GdGlobals.Players.Removed += OnPlayerRemoved;
	}

	public ObservableCollection<Player> Players { get; } = [];

	public void Dispose()
	{
		GdGlobals.Players.Added -= OnPlayerAdded;
		GdGlobals.Players.Removed -= OnPlayerRemoved;

		GC.SuppressFinalize(this);
	}

	private void OnPlayerAdded(GdPlayer gdPlayer)
	{
		var player = new Player
		{
			Name = gdPlayer.Name,
			PeerId = GdGlobals.Host.PeerIdsByPlayerId.GetValueOrDefault(gdPlayer.Id, -1)
		};

		Players.Add(player);
		_playersById[gdPlayer.Id] = player;
	}

	private void OnPlayerRemoved(GdPlayer gdPlayer)
	{
		if (_playersById.Remove(gdPlayer.Id, out var player))
			Players.Remove(player);
	}
}

public class Player
{
	public string Name { get; init; } = string.Empty;
	public int PeerId { get; init; }
}
