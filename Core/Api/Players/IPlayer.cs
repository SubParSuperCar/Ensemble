namespace EnsembleCoreRoot.Api.Players;

/// <summary>
///     The representation of a game participant tracked by <see cref="IPlayers" />.
/// </summary>
public interface IPlayer
{
	Guid Id { get; }
	string Name { get; }

	DateTimeOffset UtcCreatedAt { get; }
}
