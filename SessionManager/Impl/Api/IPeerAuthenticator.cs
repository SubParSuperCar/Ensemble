using Godot;

namespace Root.SessionManager.Api;

public interface IPeerAuthenticator
{
	// ReSharper disable once UnusedMemberInSuper.Global
	TimeSpan Timeout { get; }

	void Start(SceneMultiplayer multiplayer, bool isServer);
	void Stop(SceneMultiplayer multiplayer);

	event Action<long, string>? AuthenticationFailed;
}
