using Godot;
using Root.Autoloading;
using Root.GdCore.Players;
using Serilog;

namespace Root.Scripts.PlayerSync;

[GlobalClass]
[Autoload(Order = sbyte.MinValue + 3, FailurePolicy = AutoloadFailurePolicy.FailFast)]
public partial class PlayerSync : Node, IAutoload
{
	public void Initialize()
	{
		GSessionManager.PlayerRegistered += OnPlayerRegistered;
		GSessionManager.PlayerUnregistered += OnPlayerUnregistered;
	}

	public override void _ExitTree()
	{
		GSessionManager.PlayerRegistered -= OnPlayerRegistered;
		GSessionManager.PlayerUnregistered -= OnPlayerUnregistered;
	}

	private static void OnPlayerRegistered(int peerId, string playerId, string displayName)
	{
		if (peerId == GSessionManager.LocalPeerId)
			GPlayers.SetLocal(playerId, displayName);
		else
			GPlayers.Add(playerId, displayName);

		Log.Debug("Synced {Class} {PlayerId} for peer {PeerId}.", nameof(GdPlayer), playerId, peerId);
	}

	private static void OnPlayerUnregistered(int peerId, string playerId)
	{
		GPlayers.Remove(playerId);
		Log.Debug("Removed synced {Class} {PlayerId} for peer {PeerId}.", nameof(GdPlayer), playerId, peerId);
	}
}
