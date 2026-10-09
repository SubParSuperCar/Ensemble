using System.Buffers.Text;
using System.Globalization;
using EnsembleCoreRoot.Api.Players;

namespace EnsembleCoreRoot.Players;

/// <inheritdoc />
public sealed class Player(Guid id, string? name = null, TimeProvider? timeProvider = null) : IPlayer
{
	public Guid Id { get; } = id;
	public string Name { get; } = name ?? $"Player {ToShortGuid(id)}";

	public DateTimeOffset UtcCreatedAt { get; } = (timeProvider ?? TimeProvider.System).GetUtcNow();

	public override string ToString() =>
		string.Create(CultureInfo.InvariantCulture, $"Player(id={Id}, name={Name}, utcCreatedAt={UtcCreatedAt})");

	private static string ToShortGuid(Guid guid) => Base64Url.EncodeToString(guid.ToByteArray());
}
