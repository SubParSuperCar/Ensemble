using System.Globalization;
using Root.Core.Api.Asset;
using Root.Core.Api.Plot;

namespace Root.Core.Impl.Plot;

public class Plots : IPlots
{
	private readonly IAssets _assets;
	private readonly int? _defaultMaxInstanceCount;
	private readonly int? _defaultMaxOccupantCount;
	private readonly Dictionary<int, IPlot> _plotsById = [];

	public Plots(
		IAssets assets,
		int? defaultMaxOccupantCount = null,
		int? defaultMaxInstanceCount = null)
	{
		if (defaultMaxOccupantCount is { } occupantCount)
			ArgumentOutOfRangeException.ThrowIfNegative(occupantCount);

		if (defaultMaxInstanceCount is { } instanceCount)
			ArgumentOutOfRangeException.ThrowIfNegative(instanceCount);

		_assets = assets;
		_defaultMaxOccupantCount = defaultMaxOccupantCount;
		_defaultMaxInstanceCount = defaultMaxInstanceCount;
	}

	internal OccupantRegistry Occupants
	{
		get => field ??= new OccupantRegistry();
		init;
	}

	public IReadOnlyDictionary<int, IPlot> All => _plotsById;
	public bool IsLocked { get; private set; }

	public event Action<IPlot>? Added;
	public event Action<IPlot>? Removed;

	public IPlot Add(int id, int? maxOccupantCount = null, int? maxInstanceCount = null)
	{
		if (IsLocked)
			throw new InvalidOperationException("Plots registry is locked");

		ArgumentOutOfRangeException.ThrowIfNegative(id);

		if (maxOccupantCount is { } occupantCount)
			ArgumentOutOfRangeException.ThrowIfNegative(occupantCount);

		if (maxInstanceCount is { } instanceCount)
			ArgumentOutOfRangeException.ThrowIfNegative(instanceCount);

		if (_plotsById.ContainsKey(id))
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Plot with id {id} already exists"));

		var plot = new Plot(
			id,
			_assets,
			maxOccupantCount ?? _defaultMaxOccupantCount,
			maxInstanceCount ?? _defaultMaxInstanceCount);

		_plotsById.Add(id, plot);
		Added?.Invoke(plot);

		return plot;
	}

	public void SetPlot(Guid playerId, int? plotId = null)
	{
		if (!Occupants.TryGet(playerId, out var occupant))
			throw new InvalidOperationException($"Occupant with player id {playerId} not found");

		IPlot? plot = null;

		if (plotId is { } id && !_plotsById.TryGetValue(id, out plot))
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Plot with id {plotId} not found"));

		if (occupant.Plot is { } current)
		{
			if (ReferenceEquals(plot, current))
				return;

			current.Occupants.Remove(occupant);
		}

		(plot as Plot)?.Occupants.Add(occupant);
	}

	public IOccupant GetOccupant(Guid playerId) =>
		Occupants.TryGet(playerId, out var occupant)
			? occupant
			: throw new InvalidOperationException($"Occupant with player id {playerId} not found");

	public void Lock() => IsLocked = true;

	internal void Reset()
	{
		foreach (var (id, plot) in _plotsById.ToDictionary())
		{
			_plotsById.Remove(id);
			Removed?.Invoke(plot);
		}

		IsLocked = false;
	}
}
