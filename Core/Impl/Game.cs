using Root.Core.Api;
using Root.Core.Api.Asset;
using Root.Core.Api.Player;
using Root.Core.Api.Plot;
using Root.Core.Impl.Asset;
using Root.Core.Impl.Player;
using Root.Core.Impl.Plot;

namespace Root.Core.Impl;

public class Game : IGame
{
	public Game(
		Guid? localPlayerId = null,
		string? localPlayerName = null,
		int? defaultMaxOccupantCount = null,
		int? defaultMaxInstanceCount = null)
	{
		Players = new Players();
		Assets = new Assets();

		var occupants = new OccupantRegistry();

		Players.Added += occupants.Add;
		Players.Removed += occupants.Remove;

		if (localPlayerId is { } id)
		{
			var local = Players.Add(id, localPlayerName);
			Players.SetLocal(local.Id);
		}

		Plots = new Plots(
			Assets,
			occupants,
			defaultMaxOccupantCount,
			defaultMaxInstanceCount);
	}

	public IPlayers Players { get; }
	public IAssets Assets { get; }
	public IPlots Plots { get; }
}
