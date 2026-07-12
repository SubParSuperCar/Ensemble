using Root.Core.Api.Plot;

namespace Root.Core.Impl.Plot;

public class Occupants : IOccupants
{
	private readonly Dictionary<Guid, IOccupant> _occupantsByPlayerId = [];
	private readonly Plot _plot;

	public Occupants(Plot plot, int? maxCount = null)
	{
		// Ensure values like these are sane
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

	internal void Add(Occupant occupant, bool setOwner = true)
	{
		_occupantsByPlayerId.Add(occupant.Player.Id, occupant);
		occupant.SetPlot(_plot);

		// By default, handle updating the Owner, but skip it if the caller specifically indicated not to
		if (Owner is null && setOwner)
			SetOwner(occupant.Player.Id);

		Added?.Invoke(occupant);
	}

	internal void Remove(Occupant occupant, bool setOwner = true)
	{
		// Update the Owner similarly to in the Add method, but find the next most suitable (oldest?) runner-up to promote, or set it to null
		// To determine seniority, it could be based on how long they've been occupying the Plot, or how long they've been in the session
		if (ReferenceEquals(occupant, Owner))
			SetOwner(_occupantsByPlayerId.Count > 1 && setOwner
				? _occupantsByPlayerId.Values.First(o => !ReferenceEquals(o, occupant)).Player.Id
				: null);

		_occupantsByPlayerId.Remove(occupant.Player.Id);
		occupant.SetPlot(null);

		Removed?.Invoke(occupant);
	}
}
