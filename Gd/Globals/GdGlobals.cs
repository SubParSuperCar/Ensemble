using Root.Core.Gd;
using Root.Core.Gd.Asset;
using Root.Core.Gd.Player;
using Root.Core.Gd.Plot;
using Root.Gd.Asset;
using Root.Gd.Player;
using Root.Gd.Plot;
using Root.Session.Gd;

namespace Root.Gd.Globals;

public static class GdGlobals
{
	public static GdCore Core =>
		GdCore.Instance ?? throw new InvalidOperationException($"{nameof(GdCore)} has not been instantiated");

	public static GdPlayers Players => Core.Players;
	public static GdAssets Assets => Core.Assets;
	public static GdPlots Plots => Core.Plots;

	public static GdSession Session =>
		GdSession.Instance ??
		throw new InvalidOperationException($"{nameof(GdSession)} has not been instantiated");

	public static PlayerManager PlayerManager { get; set; } = null!;
	public static AssetManager AssetManager { get; set; } = null!;
	public static PlotManager PlotManager { get; set; } = null!;
}
