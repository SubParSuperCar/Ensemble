using EnsembleRoot.Common.Time;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.GdCore.Players;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Chat;
using EnsembleRoot.Scripts.Players;
using EnsembleRoot.Scripts.Plots;
using EnsembleRoot.Sessions;
using EnsembleRoot.Tooling;

namespace EnsembleRoot.Globals;

/// <summary>
///     Ensemble's main components, globally accessible. Each accessor throws a descriptive
///     <see cref="InvalidOperationException" /> instead of returning null while its component doesn't exist.
/// </summary>
public static class Globals
{
	private const string MainLifetime = "It is main.tscn's root script, so it exists while main.tscn is in the tree.";

	private const string AutoloadLifetime =
		"It is an autoload, so it exists from when autoloads load (see Main.AutoloadsReady) until shutdown, " +
		"unless it failed to load (see the log).";

	private const string ClientAutoloadLifetime =
		"It is a client-only autoload, so it never exists on headless servers; otherwise, it exists from when " +
		"autoloads load (see Main.AutoloadsReady) until shutdown, unless it failed to load (see the log).";

	private const string WorldLifetime =
		"It belongs to the world scene, which only exists during an active session (see SessionManager.IsActive).";

	private const string CoreLifetime = "GdCore creates it when it initializes, right after it loads.";

	/// <inheritdoc cref="Main" />
	public static Main GMain => Main.Instance ?? throw Unavailable(nameof(Main), MainLifetime);

	/// <inheritdoc cref="GdCore.GdCore" />
	public static GdCore.GdCore GCore => GdCore.GdCore.Instance ?? throw Unavailable(nameof(GdCore), AutoloadLifetime);

	/// <inheritdoc cref="GdPlayers" />
	public static GdPlayers GPlayers => GCore.Players ?? throw Unavailable(nameof(GdPlayers), CoreLifetime);

	/// <inheritdoc cref="GdAssets" />
	public static GdAssets GAssets => GCore.Assets ?? throw Unavailable(nameof(GdAssets), CoreLifetime);

	/// <inheritdoc cref="GdPlots" />
	public static GdPlots GPlots => GCore.Plots ?? throw Unavailable(nameof(GdPlots), CoreLifetime);

	/// <inheritdoc cref="SessionManager" />
	public static SessionManager GSessionManager =>
		SessionManager.Instance ?? throw Unavailable(nameof(SessionManager), AutoloadLifetime);

	/// <inheritdoc cref="ChatManager" />
	public static ChatManager GChatManager =>
		ChatManager.Instance ?? throw Unavailable(nameof(ChatManager), AutoloadLifetime);

	/// <inheritdoc cref="PlayerManager" />
	public static PlayerManager GPlayerManager
	{
		get => field ?? throw Unavailable(nameof(PlayerManager), WorldLifetime);
		set;
	} = null!;

	/// <inheritdoc cref="AssetManager" />
	public static AssetManager GAssetManager
	{
		get => field ?? throw Unavailable(nameof(AssetManager), WorldLifetime);
		set;
	} = null!;

	/// <inheritdoc cref="PlotManager" />
	public static PlotManager GPlotManager
	{
		get => field ?? throw Unavailable(nameof(PlotManager), WorldLifetime);
		set;
	} = null!;

	/// <inheritdoc cref="ToolManager" />
	public static ToolManager GToolManager =>
		ToolManager.Instance ?? throw Unavailable(nameof(ToolManager), ClientAutoloadLifetime);

	/// <inheritdoc cref="WrappedTimeProvider" />
	public static WrappedTimeProvider GTimeProvider { get; } = new();

	private static InvalidOperationException Unavailable(string component, string lifetime) =>
		new($"{component} is unavailable. {lifetime}");
}
