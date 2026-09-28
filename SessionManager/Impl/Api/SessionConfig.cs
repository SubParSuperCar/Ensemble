namespace EnsembleRoot.SessionManager.Api;

public interface ISessionConfig
{
	int Port { get; }
	string? Password { get; }
}

public sealed record HostConfig(
	int Port,
	string? Password = null,
	int? MaxClientCount = null,
	bool IsDedicated = false) : ISessionConfig;

public sealed record JoinConfig(
	string Address,
	int Port,
	string? Password = null) : ISessionConfig;
