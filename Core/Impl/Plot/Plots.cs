using System.Globalization;
using Root.Core.Api.Asset;
using Root.Core.Api.Plot;

namespace Root.Core.Impl.Plot;

public class Plots : IPlots
{
	private readonly IAssets _assets;
	private readonly int? _defaultMaxInstanceCount;
	private readonly int? _defaultMaxOccupantCount;
	private readonly Dictionary<int, IPlot> _plots = [];
	private readonly OccupantRegistry _registry;

	public Plots(
		IAssets assets,
		OccupantRegistry registry,
		int? defaultMaxOccupantCount = null,
		int? defaultMaxInstanceCount = null)
	{
		if (defaultMaxOccupantCount.HasValue)
			ArgumentOutOfRangeException.ThrowIfNegative(defaultMaxOccupantCount.Value);

		if (defaultMaxInstanceCount.HasValue)
			ArgumentOutOfRangeException.ThrowIfNegative(defaultMaxInstanceCount.Value);

		_assets = assets;
		_registry = registry;
		_defaultMaxOccupantCount = defaultMaxOccupantCount;
		_defaultMaxInstanceCount = defaultMaxInstanceCount;
	}

	public IReadOnlyDictionary<int, IPlot> All => _plots;
	public bool IsLocked { get; private set; }

	public event Action<IPlot>? Added;

	public IPlot Add(int id, int? maxOccupantCount = null, int? maxInstanceCount = null)
	{
		if (IsLocked)
			throw new InvalidOperationException("Plots registry is locked");

		ArgumentOutOfRangeException.ThrowIfNegative(id);

		if (maxOccupantCount.HasValue)
			ArgumentOutOfRangeException.ThrowIfNegative(maxOccupantCount.Value);

		if (maxInstanceCount.HasValue)
			ArgumentOutOfRangeException.ThrowIfNegative(maxInstanceCount.Value);

		if (_plots.ContainsKey(id))
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Plot with id {id} already exists"));

		var plot = new Plot(
			id,
			_assets,
			maxOccupantCount ?? _defaultMaxOccupantCount,
			maxInstanceCount ?? _defaultMaxInstanceCount);

		_plots.Add(id, plot);
		Added?.Invoke(plot);

		return plot;
	}

	public void SetPlot(Guid playerId, int? plotId = null)
	{
		if (!_registry.TryGet(playerId, out var occupant))
			throw new InvalidOperationException($"Occupant with player id {playerId} not found");

		IPlot? plot = null;

		if (plotId is { } id && !_plots.TryGetValue(id, out plot))
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

	public IOccupant GetOccupant(Guid playerId)
		=> _registry.TryGet(playerId, out var occupant)
			? occupant
			: throw new InvalidOperationException($"Occupant with player id {playerId} not found");

	public void Lock() => IsLocked = true;

	public void Reset()
	{
		_plots.Clear();
		IsLocked = false;
	}
}
