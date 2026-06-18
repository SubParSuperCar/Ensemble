using Godot;
using Root.Session.Api;

namespace Root.Session.Impl;

public class SinglePlayerSession(SceneMultiplayer multiplayer) : ISession
{
	public SessionMode Mode => SessionMode.SinglePlayer;

	public bool IsServer => true;
	public bool IsActive { get; private set; }

	public DateTime UtcStartedAt { get; } = DateTime.UtcNow;

	public event Action? Started;
	public event Action? Stopped;

#pragma warning disable CS0067
	public event Action<string>? Failed;
#pragma warning restore CS0067

	public void StartSession()
	{
		if (IsActive)
			return;

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
