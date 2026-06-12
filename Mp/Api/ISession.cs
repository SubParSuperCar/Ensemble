namespace Root.Mp.Api;

public interface ISession
{
	SessionMode Mode { get; }

	bool IsServer { get; }
	bool IsActive { get; }

	DateTime UtcStartedAt { get; }

	void StartSession();
	void StopSession();

	event Action Started;
	event Action Stopped;

	event Action<string> Failed;
}
