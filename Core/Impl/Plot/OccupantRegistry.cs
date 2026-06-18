using Root.Core.Api.Player;

namespace Root.Core.Impl.Plot;

public class OccupantRegistry
{
	private readonly Dictionary<Guid, Occupant> _byPlayerId = [];

	public bool TryGet(Guid playerId, out Occupant occupant)
		=> _byPlayerId.TryGetValue(playerId, out occupant!);

	public void Add(IPlayer player)
	{
		if (_byPlayerId.ContainsKey(player.Id))
			throw new InvalidOperationException($"Occupant for player with id {player.Id} already exists");

		_byPlayerId.Add(player.Id, new Occupant(player));
	}

	public void Remove(IPlayer player)
	{
		if (!TryGet(player.Id, out var occupant))
			return;

		occupant.Plot?.Occupants.Remove(occupant);
		_byPlayerId.Remove(player.Id);
	}
}
