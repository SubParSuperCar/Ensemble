using EnsembleRoot.Sessions.Api;
using Godot;

namespace EnsembleRoot.Sessions.Transports;

public sealed class OfflineSession(SceneMultiplayer multiplayer) : ISession
{
	public SessionMode Mode => SessionMode.SinglePlayer;
	public bool IsServer => true;

	public ISessionConfig? Config => null;

	public event Action? Started;

	event Action<string> ISession.Failed { add { } remove { } }

	public void StartSession()
	{
		multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();
		Started?.Invoke();
	}

	public Task StopSession()
	{
		multiplayer.MultiplayerPeer = null;
		return Task.CompletedTask;
	}

	public string GetAddress(int peerId) => string.Empty;
	public int GetPing(int peerId) => 0;
}
