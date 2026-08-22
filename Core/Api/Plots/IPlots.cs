using System.Diagnostics.CodeAnalysis;

namespace CoreRoot.Api.Plots;

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

	void SetPlot(Guid playerId, int? plotId = null, bool resolveOwnerIfNullOrRelinquishing = false);
	bool TryGetOccupant(Guid playerId, [NotNullWhen(true)] out IOccupant? occupant);

	void Lock();
}
