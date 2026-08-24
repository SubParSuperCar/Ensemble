using Godot;

namespace Root.SessionManager.Api;

public interface IPeerAuthenticator
{
	TimeSpan Timeout { get; }

	void StartAuth(SceneMultiplayer multiplayer, bool isServer);
	void StopAuth(SceneMultiplayer multiplayer);

	event Action<long, string>? AuthenticationFailed;
}
