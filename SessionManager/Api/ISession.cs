namespace Root.SessionManager.Api;

// I plan on making it so there's a finite difference between a peer being connected and established
// For the sake of robustness and compatibility with authentication
public interface ISession
{
	SessionMode Mode { get; }

	bool IsServer { get; }
	bool IsActive { get; }

	DateTimeOffset UtcStartedAt { get; }

	event Action Started;
	event Action Stopped;

	event Action<string> Failed;

	void StartSession();
	void StopSession();
}
