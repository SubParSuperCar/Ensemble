using Godot;

namespace Root.SessionManager.Api;

public interface IPeerAuthenticator
{
	TimeSpan Timeout { get; }

	event Action<long, string>? AuthenticationFailed;

	void StartAuth(SceneMultiplayer multiplayer, bool isServer);
	void StopAuth(SceneMultiplayer multiplayer);
}
