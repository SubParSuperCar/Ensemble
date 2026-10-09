using Godot;

namespace EnsembleRoot.Sessions.Api;

public interface IPeerAuthenticator
{
	event Action<string> Failed;

	void StartAuth(SceneMultiplayer multiplayer, bool isServer);
	void StopAuth(SceneMultiplayer multiplayer);
}
