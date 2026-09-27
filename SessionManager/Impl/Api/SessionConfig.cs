namespace EnsembleRoot.SessionManager.Api;

public interface ISessionConfig
{
	int Port { get; }
	IPeerAuthenticator Authenticator { get; }
}

public sealed record HostConfig(
	int Port,
	IPeerAuthenticator Authenticator,
	int? MaxClientCount = null) : ISessionConfig;

public sealed record JoinConfig(
	string Address,
	int Port,
	IPeerAuthenticator Authenticator) : ISessionConfig;
