using Godot;

namespace EnsembleRoot.Sessions.Actions;

public readonly record struct ActionSource(int PeerId, string PlayerId)
{
	/// <summary>Whether the host sent the action, who has authority over the whole session.</summary>
	public bool IsHost => PeerId == MultiplayerPeer.TargetPeerServer;
}
