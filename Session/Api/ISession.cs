namespace Root.Session.Api;

public interface ISession
{
	// ReSharper disable once UnusedMember.Global
	SessionMode Mode { get; }

	bool IsServer { get; }
	bool IsActive { get; }

	DateTime UtcStartedAt { get; }

	event Action Started;
	event Action Stopped;

	event Action<string> Failed;

	void StartSession();
	void StopSession();
}
