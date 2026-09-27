using Godot;

namespace EnsembleRoot.SessionManager.Api;

public interface IPeerAuthenticator
{
	event Action<string> Failed;

	void Start(SceneMultiplayer multiplayer, bool isServer);
	void Stop(SceneMultiplayer multiplayer);
}
