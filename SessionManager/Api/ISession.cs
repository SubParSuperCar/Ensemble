namespace Root.SessionManager.Api;

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
