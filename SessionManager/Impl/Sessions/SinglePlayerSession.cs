using EnsembleRoot.SessionManager.Api;
using Godot;

namespace EnsembleRoot.SessionManager.Sessions;

public sealed class SinglePlayerSession(SceneMultiplayer multiplayer) : ISession
{
	public SessionMode Mode => SessionMode.SinglePlayer;
	public bool IsServer => true;

	public event Action? Started;

	event Action<string> ISession.Failed { add { } remove { } }

	public void StartSession()
	{
		multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();
		Started?.Invoke();
	}

	public void StopSession() => multiplayer.MultiplayerPeer = null;
}
