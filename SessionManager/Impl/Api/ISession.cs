namespace Root.SessionManager.Api;

public interface ISession
{
	SessionMode Mode { get; }

	// Should IsActive be on top or IsServer?
	bool IsServer { get; }
	bool IsActive { get; }

	// Lets us track how long the session has been running for.
	DateTimeOffset UtcStartedAt { get; }

	event Action Started;
	event Action Stopped;

	event Action<string> Failed;

	// Same thing here as IPeerAuthenticator.
	void StartSession();
	void StopSession();
}
