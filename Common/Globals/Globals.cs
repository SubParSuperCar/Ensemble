using Root.GdCore.Assets;
using Root.GdCore.Players;
using Root.GdCore.Plots;

// ReSharper disable UnusedMember.Global

namespace Root.Common.Globals;

public static class Globals
{
	// ReSharper disable once MemberCanBePrivate.Global
	public static GdCore.GdCore GCore =>
		GdCore.GdCore.Instance ?? throw new InvalidOperationException($"{nameof(GdCore)} is null.");

	public static GdPlayers GPlayers =>
		GCore.Players ?? throw new InvalidOperationException($"{nameof(GdCore.Players)} is null.");

	public static GdAssets GAssets =>
		GCore.Assets ?? throw new InvalidOperationException($"{nameof(GdCore.Assets)} is null.");

	public static GdPlots GPlots =>
		GCore.Plots ?? throw new InvalidOperationException($"{nameof(GdCore.Plots)} is null.");

	public static SessionManager.SessionManager GSessionManager =>
		SessionManager.SessionManager.Instance ??
		throw new InvalidOperationException($"{nameof(SessionManager.SessionManager)} is null.");
}
