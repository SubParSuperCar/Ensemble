using System.Diagnostics.CodeAnalysis;

namespace EnsembleCoreRoot.Api.Plots;

/// <summary>
///     The registry of <see cref="IPlot" /> objects and the assignment of players to them as <see cref="IOccupant" />
///     objects. Should be locked once registration is complete.
/// </summary>
public interface IPlots
{
	IReadOnlyDictionary<int, IPlot> All { get; }
	bool IsLocked { get; }

	event Action<IPlot> Added;
	event Action<IPlot> Removed;

	IPlot Add(
		int id,
		int? maxOccupantCount = null,
		int? maxInstanceCount = null);

	void SetPlot(Guid playerId, int? plotId = null, bool shouldResolveOwnerIfNullOrRelinquishing = false);
	bool TryGetOccupant(Guid playerId, [NotNullWhen(true)] out IOccupant? occupant);

	void Lock();
}
