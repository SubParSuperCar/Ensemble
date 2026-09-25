using EnsembleRoot.Common.Time;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.GdCore.Players;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Players;
using EnsembleRoot.Scripts.Plots;
using EnsembleRoot.Tooling;
using DateTime = EnsembleRoot.Common.Time.DateTime;

namespace EnsembleRoot.Common.Globals;

// All members here should be mostly GDScript-friendly, especially GdCore and SessionManager
public static class Globals
{
	/// <inheritdoc cref="Main" />
	public static Main GMain => Main.Instance ?? throw new InvalidOperationException($"{nameof(Main)} is null.");

	/// <inheritdoc cref="GdCore" />
	public static GdCore.GdCore GCore =>
		GdCore.GdCore.Instance ?? throw new InvalidOperationException($"{nameof(GdCore)} is null.");

	/// <inheritdoc cref="GdPlayers" />
	public static GdPlayers GPlayers =>
		GCore.Players ?? throw new InvalidOperationException($"{nameof(GdPlayers)} is null.");

	/// <inheritdoc cref="GdAssets" />
	public static GdAssets GAssets =>
		GCore.Assets ?? throw new InvalidOperationException($"{nameof(GdAssets)} is null.");

	/// <inheritdoc cref="GdPlots" />
	public static GdPlots GPlots => GCore.Plots ?? throw new InvalidOperationException($"{nameof(GdPlots)} is null.");

	/// <inheritdoc cref="SessionManager.SessionManager" />
	public static SessionManager.SessionManager GSessionManager =>
		SessionManager.SessionManager.Instance ??
		throw new InvalidOperationException($"{nameof(SessionManager.SessionManager)} is null.");

	public static PlayerManager GPlayerManager
	{
		get => field ?? throw new InvalidOperationException($"{nameof(PlayerManager)} is null.");
		set;
	} = null!;

	public static AssetManager GAssetManager
	{
		get => field ?? throw new InvalidOperationException($"{nameof(AssetManager)} is null.");
		set;
	} = null!;

	public static PlotManager GPlotManager
	{
		get => field ?? throw new InvalidOperationException($"{nameof(PlotManager)} is null.");
		set;
	} = null!;

	public static ToolManager GToolManager =>
		ToolManager.Instance ?? throw new InvalidOperationException($"{nameof(ToolManager)} is null.");

	public static WrappedTimeProvider GTimeProvider => DateTime.TimeProvider;
}
