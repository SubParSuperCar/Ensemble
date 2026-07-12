using Root.Core.Api.Asset;
using Root.Core.Api.Player;
using Root.Core.Api.Plot;

// ReSharper disable UnusedMemberInSuper.Global

namespace Root.Core.Api;

// These APIs act as a contract between the agnostic Core and the Godot-facing bridge
public interface ICore
{
	IPlayers Players { get; }
	IAssets Assets { get; }
	IPlots Plots { get; }

	void Reset();
}
