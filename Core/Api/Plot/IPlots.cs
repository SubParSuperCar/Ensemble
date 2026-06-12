namespace Root.Core.Api.Plot;

public interface IPlots
{
	IReadOnlyDictionary<int, IPlot> All { get; }
	bool IsLocked { get; }

	IPlot Add(
		int id,
		int? maxOccupantCount = null,
		int? maxInstanceCount = null);

	event Action<IPlot> Added;

	void SetPlot(Guid playerId, int? plotId = null);
	IOccupant GetOccupant(Guid playerId);

	void Lock();
	void Reset();
}
