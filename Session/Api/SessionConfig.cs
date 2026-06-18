namespace Root.Session.Api;

public interface ISessionConfig;

public record HostConfig(int Port, string? Password = null, int? MaxPlayerCount = null) : ISessionConfig;

public record JoinConfig(string Address, int Port, string? Password = null) : ISessionConfig;
