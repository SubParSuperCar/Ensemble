namespace Root.Core.Api.Player;

public interface IPlayer
{
	Guid Id { get; }
	string Name { get; }

	DateTimeOffset UtcCreatedAt { get; }
}
