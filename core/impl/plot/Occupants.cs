using Root.Core.Api.Plot;

namespace Root.Core.Impl.Plot;

public class Occupants : IOccupants
{
	private readonly Dictionary<Guid, IOccupant> _occupantsByPlayerId = [];
	private readonly Plot _plot;

	public Occupants(Plot plot, int? maxCount = null)
	{
		if (maxCount is { } count)
			ArgumentOutOfRangeException.ThrowIfNegative(count);

		_plot = plot;
		MaxCount = maxCount ?? Unlimited;
	}

	public IReadOnlyDictionary<Guid, IOccupant> All => _occupantsByPlayerId;
	public int MaxCount { get; }

	public IOccupant? Owner { get; private set; }

	public event Action<IOccupant>? Added;
	public event Action<IOccupant>? Removed;
	public event Action<IOccupant?>? OwnerChanged;

	public void SetOwner(Guid? playerId = null)
	{
		IOccupant? occupant = null;

		if (playerId is { } id && !_occupantsByPlayerId.TryGetValue(id, out occupant))
			throw new KeyNotFoundException($"Occupant with player id {playerId} not found");

		if (ReferenceEquals(occupant, Owner))
			return;

		Owner = occupant;
		OwnerChanged?.Invoke(occupant);
	}

	public void Clear()
	{
		foreach (var occupant in _occupantsByPlayerId.Values.ToArray())
			Remove((Occupant)occupant);
	}

	// We might want to remove setOwner args? But this also helps make sure the event fires at maybe a better time
	// We may have to update API for some of this? Maybe? But also probably not because these are internal concerns.
	internal void Add(Occupant occupant, bool setOwner = true)
	{
		_occupantsByPlayerId.Add(occupant.Player.Id, occupant);
		occupant.SetPlot(_plot);

		// This should be moved to GdCore as per comment.
		if (Owner is null && setOwner)
			SetOwner(occupant.Player.Id);

		Added?.Invoke(occupant);
	}

	internal void Remove(Occupant occupant, bool setOwner = true)
	{
		// This should only set owner to null, never to another unless GdCore is exchanging for another
		// occupant to take over (relinquishing ownership) as per logical.
		if (ReferenceEquals(occupant, Owner))
			SetOwner(_occupantsByPlayerId.Count > 1 && setOwner
				? _occupantsByPlayerId.Values.First(other => !ReferenceEquals(other, occupant)).Player.Id
				: null);

		_occupantsByPlayerId.Remove(occupant.Player.Id);
		occupant.SetPlot(null);
		// (Above ^) Is there some way to only call this if it's actually being set to null, not an exchange?

		// This can stay because obviously it's going to be removed from *this* plot nonetheless.
		Removed?.Invoke(occupant);
	}
}
