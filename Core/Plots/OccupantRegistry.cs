using System.Diagnostics.CodeAnalysis;
using CoreRoot.Api.Players;

namespace CoreRoot.Plots;

internal sealed class OccupantRegistry
{
	private readonly Dictionary<Guid, Occupant> _occupantsByPlayerId = [];

	public bool TryGet(Guid playerId, [NotNullWhen(true)] out Occupant? occupant) =>
		_occupantsByPlayerId.TryGetValue(playerId, out occupant);

	public void Add(IPlayer player)
	{
		if (_occupantsByPlayerId.ContainsKey(player.Id))
			throw new InvalidOperationException($"Occupant for player with id {player.Id} already exists");

		_occupantsByPlayerId.Add(player.Id, new Occupant(player));
	}

	public void Remove(IPlayer player)
	{
		if (!TryGet(player.Id, out var occupant))
			return;

		occupant.Plot?.Occupants.Remove(occupant);
		_occupantsByPlayerId.Remove(player.Id);
	}
}
