namespace EnsembleCoreRoot.Api.Plots;

/// <summary>
///     The <see cref="IOccupant" /> objects on a single <see cref="IPlot" />,
///     including which one, if any, is the owner.
/// </summary>
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
