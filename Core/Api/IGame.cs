using Root.Core.Api.Asset;
using Root.Core.Api.Player;
using Root.Core.Api.Plot;

// ReSharper disable UnusedMemberInSuper.Global

namespace Root.Core.Api;

public interface IGame
{
	IPlayers Players { get; }
	IAssets Assets { get; }
	IPlots Plots { get; }

	void Reset();
}
