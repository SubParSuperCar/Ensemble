namespace Root.Core.Api.Plot;

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

	void SetPlot(Guid playerId, int? plotId = null);
	IOccupant GetOccupant(Guid playerId);

	void Lock();
}
