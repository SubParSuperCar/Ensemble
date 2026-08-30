using Godot;
using Root.SessionManager.Api;

namespace Root.SessionManager.Sessions;

public sealed class SinglePlayerSession(SceneMultiplayer multiplayer) : ISession
{
	public SessionMode Mode => SessionMode.SinglePlayer;

	public bool IsServer => true;
	public bool IsActive { get; private set; }

	public DateTimeOffset UtcStartedAt { get; } = GTimeProvider.GetUtcNow();

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
