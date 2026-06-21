using Root.Core.Gd;
using Root.Core.Gd.Asset;
using Root.Core.Gd.Player;
using Root.Core.Gd.Plot;
using Root.Gd.Asset;
using Root.Gd.Player;
using Root.Gd.Plot;
using Root.Host.Gd;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Root.Gd.Globals;

public static class GdGlobals
{
	// ReSharper disable once MemberCanBePrivate.Global
	public static GdCore Core =>
		GdCore.Instance ?? throw new InvalidOperationException($"{nameof(GdCore)} is null");

	public static GdPlayers Players => Core.Players;
	public static GdAssets Assets => Core.Assets;
	public static GdPlots Plots => Core.Plots;

	public static GdHost Host =>
		GdHost.Instance ?? throw new InvalidOperationException($"{nameof(GdHost)} is null");

	public static PlayerManager PlayerManager { get; set; } = null!;
	public static AssetManager AssetManager { get; set; } = null!;
	public static PlotManager PlotManager { get; set; } = null!;
}
