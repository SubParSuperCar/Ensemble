using EnsembleCoreRoot.Api;
using EnsembleCoreRoot.Api.Assets;
using EnsembleCoreRoot.Api.Players;
using EnsembleCoreRoot.Api.Plots;
using EnsembleCoreRoot.Plots;

namespace EnsembleCoreRoot;

/// <inheritdoc />
/// <remarks>
///     Core is non-authoritative; max counts are metadata only and are not enforced.
/// </remarks>
public sealed class Core : ICore
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
		var occupants = new OccupantRegistry();

		_players = new Players.Players(timeProvider);
		_players.Added += occupants.Add;
		_players.Removed += occupants.Remove;

		if (localPlayerId is { } id)
		{
			_players.Add(id, localPlayerName);
			_players.SetLocal(id);
		}

		_assets = new Assets.Assets();
		_plots = new Plots.Plots(_assets, defaultMaxOccupantCount, defaultMaxInstanceCount) { Occupants = occupants };
	}

	public IPlayers Players => _players;
	public IAssets Assets => _assets;
	public IPlots Plots => _plots;

	public void Reset()
	{
		_players.Reset();
		_plots.Reset();
		_assets.Reset();
	}
}
