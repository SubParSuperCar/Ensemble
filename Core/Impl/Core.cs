using Root.Core.Api;
using Root.Core.Api.Asset;
using Root.Core.Api.Player;
using Root.Core.Api.Plot;
using Root.Core.Impl.Asset;
using Root.Core.Impl.Player;
using Root.Core.Impl.Plot;

namespace Root.Core.Impl;

public class Core : ICore
{
	private readonly Assets _assets;
	private readonly Players _players;
	private readonly Plots _plots;

	public Core(
		Guid? localPlayerId = null,
		string? localPlayerName = null,
		int? defaultMaxOccupantCount = null,
		int? defaultMaxInstanceCount = null)
	{
		_players = new Players();
		_assets = new Assets();

		var occupants = new OccupantRegistry();

		_players.Added += occupants.Add;
		_players.Removed += occupants.Remove;

		if (localPlayerId is { } id)
		{
			var local = _players.Add(id, localPlayerName);
			_players.SetLocal(local.Id);
		}

		_plots = new Plots(
			_assets,
			defaultMaxOccupantCount,
			defaultMaxInstanceCount)
		{
			Occupants = occupants
		};
	}

	public IPlayers Players => _players;
	public IAssets Assets => _assets;
	public IPlots Plots => _plots;

	public void Reset()
	{
		_plots.Reset();
		_assets.Reset();
		_players.Reset();
	}
}
