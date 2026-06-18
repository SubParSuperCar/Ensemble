namespace Root.Core.Api.Plot;

public interface IOccupants
{
	IReadOnlyDictionary<Guid, IOccupant> All { get; }
	int MaxCount { get; }

	IOccupant? Owner { get; }

	event Action<IOccupant> Added;
	event Action<IOccupant> Removed;
	event Action<IOccupant?> OwnerChanged;

	void SetOwner(Guid? playerId = null);

	void Clear();
}
