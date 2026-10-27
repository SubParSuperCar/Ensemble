using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using EnsembleCoreRoot.Api.Assets;
using EnsembleCoreRoot.Api.Plots;

namespace EnsembleCoreRoot.Plots;

/// <inheritdoc />
internal sealed class Plots : IPlots
{
	private readonly IAssets _assets;
	private readonly int? _defaultMaxInstanceCount;
	private readonly int? _defaultMaxOccupantCount;
	private readonly Dictionary<int, IPlot> _plotsById = [];

	public Plots(IAssets assets, int? defaultMaxOccupantCount = null, int? defaultMaxInstanceCount = null)
	{
		ThrowIfInvalidMaxCount(defaultMaxOccupantCount);
		ThrowIfInvalidMaxCount(defaultMaxInstanceCount);

		_assets = assets;
		_defaultMaxOccupantCount = defaultMaxOccupantCount;
		_defaultMaxInstanceCount = defaultMaxInstanceCount;
	}

	internal OccupantRegistry Occupants { get; } = new();

	public IReadOnlyDictionary<int, IPlot> All => _plotsById;
	public bool IsLocked { get; private set; }

	public event Action<IPlot>? Added;
	public event Action<IPlot>? Removed;

	public IPlot Add(int id, int? maxOccupantCount = null, int? maxInstanceCount = null)
	{
		if (IsLocked)
			throw new InvalidOperationException("Plots registry is locked.");

		ArgumentOutOfRangeException.ThrowIfNegative(id);

		if (_plotsById.ContainsKey(id))
			throw new InvalidOperationException(
				string.Create(CultureInfo.InvariantCulture, $"Plot with ID {id} already exists."));

		var plot = new Plot(
			id, _assets, maxOccupantCount ?? _defaultMaxOccupantCount, maxInstanceCount ?? _defaultMaxInstanceCount);

		_plotsById.Add(id, plot);
		Added?.Invoke(plot);

		return plot;
	}

	public void SetPlot(Guid playerId, int? plotId = null, bool shouldResolveOwnerIfNullOrRelinquishing = false)
	{
		if (!Occupants.TryGet(playerId, out var occupant))
			throw new KeyNotFoundException($"Occupant with player ID {playerId} not found.");

		IPlot? plot = null;

		if (plotId is { } id && !_plotsById.TryGetValue(id, out plot))
			throw new KeyNotFoundException(
				string.Create(CultureInfo.InvariantCulture, $"Plot with ID {id} not found."));

		if (occupant.Plot is { } current)
		{
			if (ReferenceEquals(plot, current))
				return;

			current.Occupants.Remove(occupant, shouldResolveOwnerIfNullOrRelinquishing, plot is not null);
		}

		((Plot?)plot)?.Occupants.Add(occupant, shouldResolveOwnerIfNullOrRelinquishing);
	}

	public bool TryGetOccupant(Guid playerId, [NotNullWhen(true)] out IOccupant? occupant)
	{
		var isFound = Occupants.TryGet(playerId, out var found);
		occupant = found;

		return isFound;
	}

	public void Lock() => IsLocked = true;

	internal void Reset()
	{
		foreach (var (id, plot) in _plotsById.ToArray())
		{
			_plotsById.Remove(id);
			Removed?.Invoke(plot);
		}

		IsLocked = false;
	}
}
