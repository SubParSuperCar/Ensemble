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

	public void Add(Occupant occupant)
	{
		_occupantsByPlayerId.Add(occupant.Player.Id, occupant);
		occupant.SetPlot(_plot);

		Added?.Invoke(occupant);
	}

	public void Remove(Occupant occupant)
	{
		_occupantsByPlayerId.Remove(occupant.Player.Id);
		occupant.SetPlot(null);

		if (ReferenceEquals(occupant, Owner))
			SetOwner();

		Removed?.Invoke(occupant);
	}
}
