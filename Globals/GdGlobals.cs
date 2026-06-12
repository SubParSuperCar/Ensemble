using Root.Core.Gd;
using Root.Core.Gd.Asset;
using Root.Core.Gd.Player;
using Root.Core.Gd.Plot;
using Root.Mp.Gd;
using Root.Scripts.Asset;
using Root.Scripts.Player;
using Root.Scripts.Plot;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Root.Globals;

public static class GdGlobals
{
	// ReSharper disable once MemberCanBePrivate.Global
	public static GdGame Game =>
		GdGame.Instance ?? throw new InvalidOperationException("GdGame has not been instantiated");

	public static GdPlayers Players => Game.Players;
	public static GdAssets Assets => Game.Assets;
	public static GdPlots Plots => Game.Plots;

	public static GdMp Mp => GdMp.Instance ?? throw new InvalidOperationException("GdMp has not been instantiated");

	public static PlayerManager PlayerManager { get; set; } = null!;
	public static AssetManager AssetManager { get; set; } = null!;
	public static PlotManager PlotManager { get; set; } = null!;
}
