using Root.Core.Api.Player;

namespace Root.Core.Impl.Plot;

public class OccupantRegistry
{
	private readonly Dictionary<Guid, Occupant> _occupants = [];

	public bool TryGet(Guid playerId, out Occupant occupant)
		=> _occupants.TryGetValue(playerId, out occupant!);

	public void Add(IPlayer player)
	{
		if (_occupants.ContainsKey(player.Id))
			throw new InvalidOperationException($"Occupant for player with id {player.Id} already exists");

		_occupants.Add(player.Id, new Occupant(player));
	}

	public void Remove(IPlayer player)
	{
		if (!TryGet(player.Id, out var occupant))
			return;

		occupant.Plot?.Occupants.Remove(occupant);
		_occupants.Remove(player.Id);
	}
}
