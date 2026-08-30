using CoreRoot.Api.Players;

namespace CoreRoot.Players;

public class Player(Guid id, string? name = null, TimeProvider? timeProvider = null) : IPlayer
{
	public Guid Id { get; } = id;
	public string Name { get; } = name ?? $"Player {ToShortGuid(id)}";

	public DateTimeOffset UtcCreatedAt { get; } = (timeProvider ?? TimeProvider.System).GetUtcNow();

	public override string ToString() => $"Player(id={Id}, name={Name}, utcCreatedAt={UtcCreatedAt})";

	private static string ToShortGuid(Guid guid) =>
		Convert.ToBase64String(guid.ToByteArray())
			.Replace('+', '-')
			.Replace('/', '_')[..22];
}
