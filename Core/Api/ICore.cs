using CoreRoot.Api.Assets;
using CoreRoot.Api.Players;
using CoreRoot.Api.Plots;

// ReSharper disable UnusedMemberInSuper.Global

namespace CoreRoot.Api;

public interface ICore
{
	IPlayers Players { get; }
	IAssets Assets { get; }
	IPlots Plots { get; }

	void Reset();
}
