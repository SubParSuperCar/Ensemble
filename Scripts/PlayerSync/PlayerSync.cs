using EnsembleRoot.Autoloading;
using EnsembleRoot.GdCore.Players;
using EnsembleRoot.Sessions;
using Godot;
using Serilog;

namespace EnsembleRoot.Scripts.PlayerSync;

/// <summary>
///     Mirrors session <see cref="Peer" /> objects as <see cref="GdPlayers" /> players,
///     keeping SessionManager and GdCore independent of each other.
/// </summary>
[GlobalClass]
[Autoload(Order = AutoloadOrder.Early + 2, FailurePolicy = AutoloadFailurePolicy.FailFast)]
public partial class PlayerSync : Node, IAutoload
{
	public void Initialize()
	{
		GSessionManager.PeerRegistered += OnPeerRegistered;
		GSessionManager.PeerUnregistered += OnPeerUnregistered;
	}

	public override void _ExitTree()
	{
		GSessionManager.PeerRegistered -= OnPeerRegistered;
		GSessionManager.PeerUnregistered -= OnPeerUnregistered;
	}

	private static void OnPeerRegistered(Peer peer)
	{
		if (peer.IsLocal)
			GPlayers.SetLocal(peer.PlayerId, peer.DisplayName);
		else
			GPlayers.Add(peer.PlayerId, peer.DisplayName);

		Log.Debug("Synced {Class} {PlayerId} for peer {PeerId}", nameof(GdPlayer), peer.PlayerId, peer.Id);
	}

	// Leaves the plot first, so ownership passes on; a disconnect isn't a choice, so the plot keeps its instances
	private static void OnPeerUnregistered(Peer peer)
	{
		GPlots.SetPlot(peer.PlayerId, None, true, false);
		GPlayers.Remove(peer.PlayerId);
		Log.Debug("Removed synced {Class} {PlayerId} for peer {PeerId}", nameof(GdPlayer), peer.PlayerId, peer.Id);
	}
}
