using CoreRoot.Api.Plots;

namespace CoreRoot.Plots;

public class Occupants : IOccupants
{
	private readonly Dictionary<Guid, IOccupant> _occupantsByPlayerId = [];
	private readonly Plot _plot;

	public Occupants(Plot plot, int? maxCount = null)
	{
		if (maxCount is { } count and not Unlimited)
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
			throw new KeyNotFoundException($"Occupant with player id {playerId} not found.");

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

	internal void Add(Occupant occupant, bool resolveOwnerIfNull = false)
	{
		_occupantsByPlayerId.Add(occupant.Player.Id, occupant);
		occupant.SetPlot(_plot);

		if (Owner is null && resolveOwnerIfNull)
			SetOwner(occupant.Player.Id);

		Added?.Invoke(occupant);
	}

	internal void Remove(Occupant occupant, bool resolveOwnerIfRelinquishing = false, bool isExchanging = false)
	{
		if (ReferenceEquals(occupant, Owner))
			SetOwner(_occupantsByPlayerId.Count > 1 && resolveOwnerIfRelinquishing
				? _occupantsByPlayerId.Values.First(other => !ReferenceEquals(other, occupant)).Player.Id
				: null);

		_occupantsByPlayerId.Remove(occupant.Player.Id);

		if (!isExchanging)
			occupant.SetPlot(null);

		Removed?.Invoke(occupant);
	}
}
