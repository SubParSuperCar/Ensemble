using CoreRoot.Api;
using CoreRoot.Api.Assets;
using CoreRoot.Api.Players;
using CoreRoot.Api.Plots;
using CoreRoot.Plots;

namespace CoreRoot;

public class Core : ICore
{
	private readonly Assets.Assets _assets;
	private readonly Players.Players _players;
	private readonly Plots.Plots _plots;

	public Core(
		Guid? localPlayerId = null,
		string? localPlayerName = null,
		int? defaultMaxOccupantCount = null,
		int? defaultMaxInstanceCount = null,
		TimeProvider? timeProvider = null)
	{
		_players = new Players.Players(timeProvider);
		_assets = new Assets.Assets();

		var occupants = new OccupantRegistry();

		_players.Added += occupants.Add;
		_players.Removed += occupants.Remove;

		if (localPlayerId is { } id)
		{
			var local = _players.Add(id, localPlayerName);
			_players.SetLocal(local.Id);
		}

		_plots = new Plots.Plots(
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
