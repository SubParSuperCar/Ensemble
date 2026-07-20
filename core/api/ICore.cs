using System.Diagnostics.CodeAnalysis;
using Root.Core.Api.Asset;
using Root.Core.Api.Player;
using Root.Core.Api.Plot;

namespace Root.Core.Api;

[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface ICore
{
	IPlayers Players { get; }
	IAssets Assets { get; }
	IPlots Plots { get; }

	void Reset();
}
