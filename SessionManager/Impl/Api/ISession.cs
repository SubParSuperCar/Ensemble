namespace EnsembleRoot.SessionManager.Api;

public interface ISession
{
	SessionMode Mode { get; }
	bool IsServer { get; }

	event Action Started;
	event Action<string> Failed;

	void StartSession();
	void StopSession();
}
