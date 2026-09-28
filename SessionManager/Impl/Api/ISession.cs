namespace EnsembleRoot.SessionManager.Api;

public interface ISession
{
	SessionMode Mode { get; }
	bool IsServer { get; }

	ISessionConfig? Config { get; }

	event Action Started;
	event Action<string> Failed;

	void StartSession();
	void StopSession();

	string GetAddress(int peerId);
	int GetPing(int peerId);
}
