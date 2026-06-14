using Root.Core.Api.Asset;
using Root.Core.Api.Player;
using Root.Core.Api.Plot;

// ReSharper disable UnusedMemberInSuper.Global

namespace Root.Core.Api;

public interface ICore
{
	IPlayers Players { get; }
	IAssets Assets { get; }
	IPlots Plots { get; }
}
