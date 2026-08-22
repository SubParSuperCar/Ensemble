using Godot;

namespace Root.SessionManager.Api;

public interface IPeerAuthenticator
{
	// ReSharper disable once UnusedMemberInSuper.Global
	TimeSpan Timeout { get; }

	// We suffix the method name with "Auth" for the same reason as not just using "Get" in GdCore; keyword clashing.
	void StartAuth(SceneMultiplayer multiplayer, bool isServer);
	void StopAuth(SceneMultiplayer multiplayer);

	event Action<long, string>? AuthenticationFailed; // Useful for UI.
}
