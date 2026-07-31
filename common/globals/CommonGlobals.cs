using Root.Core.Gd;
using Root.Core.Gd.Asset;
using Root.Core.Gd.Player;
using Root.Core.Gd.Plot;
using Root.Scripts.Asset;
using Root.Scripts.Player;
using Root.Scripts.Plot;

namespace Root.Common.Globals;

public static class CommonGlobals
{
	public static GdCore GCore => GdCore.Instance ?? throw new InvalidOperationException($"{nameof(GdCore)} is null");

	public static GdPlayers GPlayers =>
		GCore.Players ?? throw new InvalidOperationException($"{nameof(GdCore.Players)} is null");

	public static GdAssets GAssets =>
		GCore.Assets ?? throw new InvalidOperationException($"{nameof(GdCore.Assets)} is null");

	public static GdPlots GPlots =>
		GCore.Plots ?? throw new InvalidOperationException($"{nameof(GdCore.Plots)} is null");

	public static SessionManager.Gd.SessionManager GSessionManager =>
		SessionManager.Gd.SessionManager.Instance ??
		throw new InvalidOperationException($"{nameof(SessionManager.Gd.SessionManager)} is null");

	public static PlayerManager GPlayerManager
	{
		get => field ?? throw new InvalidOperationException($"{nameof(PlayerManager)} is null");
		set;
	} = null!;

	public static AssetManager GAssetManager
	{
		get => field ?? throw new InvalidOperationException($"{nameof(AssetManager)} is null");
		set;
	} = null!;

	public static PlotManager GPlotManager
	{
		get => field ?? throw new InvalidOperationException($"{nameof(PlotManager)} is null");
		set;
	} = null!;
}
