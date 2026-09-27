using Godot;

namespace EnsembleRoot.SessionManager.Api;

public interface IPeerAuthenticator
{
	event Action<string> Failed;

	void StartAuth(SceneMultiplayer multiplayer, bool isServer);
	void StopAuth(SceneMultiplayer multiplayer);
}
