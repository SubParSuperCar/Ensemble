// ReSharper disable NotAccessedPositionalProperty.Global

namespace Root.SessionManager.Api;

public interface ISessionConfig
{
	IPeerAuthenticator? Authenticator { get; }
}

public sealed record HostConfig(int Port, IPeerAuthenticator? Authenticator = null, int? MaxPlayerCount = null)
	: ISessionConfig;

public sealed record JoinConfig(string Address, int Port, IPeerAuthenticator? Authenticator = null) : ISessionConfig;
