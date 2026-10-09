namespace EnsembleRoot.Sessions.Api;

/// <summary>
///     A session transport that <see cref="SessionManager" /> drives, e.g., offline or ENet. New transports (such as
///     Steam) implement this, plus optional capabilities like <see cref="IPortMappingSession" />, and are started with
///     <see cref="SessionManager.StartSession(ISession, string?)" />.
/// </summary>
/// <remarks>
///     All members are called, and all events must be raised, on the main thread. <see cref="Failed" /> may be raised
///     during <see cref="StartSession" />; <see cref="StopSession" /> is called after any start, failed or not.
/// </remarks>
public interface ISession
{
	SessionMode Mode { get; }
	bool IsServer { get; }

	ISessionConfig? Config { get; }

	event Action Started;
	event Action<string> Failed;

	void StartSession();

	/// <returns>
	///     Cleanup that may outlive the session (e.g., releasing a router port mapping). The main thread blocks on it
	///     briefly at quit, so it must not need the main thread to complete.
	/// </returns>
	Task StopSession();

	string GetAddress(int peerId);
	int GetPing(int peerId);
}
