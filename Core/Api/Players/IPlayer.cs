namespace CoreRoot.Api.Players;

public interface IPlayer
{
	Guid Id { get; }
	string Name { get; }

	DateTimeOffset UtcCreatedAt { get; }
}
