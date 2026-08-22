using Godot;
using Root.SessionManager.Api;

namespace Root.SessionManager.Impl;

// Same as multi-player but for single player sessions. Highly modular.
public class SinglePlayerSession(SceneMultiplayer multiplayer) : ISession
{
	public SessionMode Mode => SessionMode.SinglePlayer;

	public bool IsServer => true;
	public bool IsActive { get; private set; }

	public DateTimeOffset UtcStartedAt { get; } = DateTimeOffset.UtcNow;

	public event Action? Started;
	public event Action? Stopped;

#pragma warning disable CS0067
	public event Action<string>?
		Failed; // A single player session can essentially never fail, so this event is never actually used.
#pragma warning restore CS0067

	public void StartSession()
	{
		if (IsActive)
			return;

		// I love Godot's Multiplayer API. This is too easy.
		multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();

		IsActive = true;
		Started?.Invoke();
	}

	public void StopSession()
	{
		if (!IsActive)
			return;

		multiplayer.MultiplayerPeer = null;

		IsActive = false;
		Stopped?.Invoke();
	}
}
