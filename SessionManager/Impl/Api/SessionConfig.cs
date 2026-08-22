// ReSharper disable NotAccessedPositionalProperty.Global

namespace Root.SessionManager.Api;

// This one file stores three separate resources: is that fine?
public interface ISessionConfig
{
	IPeerAuthenticator? Authenticator { get; }
}

// Annoying 120 char line limit formatting anomaly.
public sealed record HostConfig(int Port, IPeerAuthenticator? Authenticator = null, int? MaxPlayerCount = null)
	: ISessionConfig;

public sealed record JoinConfig(string Address, int Port, IPeerAuthenticator? Authenticator = null) : ISessionConfig;
