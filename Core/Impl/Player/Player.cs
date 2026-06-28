using Root.Core.Api.Player;

namespace Root.Core.Impl.Player;

public class Player(Guid id, string? name = null) : IPlayer
{
	public Guid Id { get; } = id;
	public string Name { get; } = name ?? $"Player {id}";

	public DateTimeOffset UtcCreatedAt { get; } = DateTimeOffset.UtcNow;

	public override string ToString() => $"Player(id={Id}, name={Name}, utcCreatedAt={UtcCreatedAt})";
}
