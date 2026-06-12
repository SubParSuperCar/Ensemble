using Root.Core.Api.Asset;
using Root.Core.Api.Player;
using Root.Core.Api.Plot;

namespace Root.Core.Api;

public interface IGame
{
	IPlayers Players { get; }
	IAssets Assets { get; }
	IPlots Plots { get; }
}
