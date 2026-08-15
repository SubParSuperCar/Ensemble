using Root.Core.Gd.Player;
using Root.Core.Gd.Plot;

// ReSharper disable EventNeverSubscribedTo.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Root.Common.Globals;

public static class GContext
{
	private static GdOccupant? _occupant;
	private static GdPlot? _plot;

	static GContext()
	{
		GPlayers.LocalChanged += OnLocalChanged;
		OnLocalChanged(GPlayers.Local);
	}

	public static GdPlot? Plot { get; private set; }
	public static bool? IsPlotOwner { get; private set; }
	public static bool? IsPlotSpawned { get; private set; }

	public static event Action<GdPlot?>? PlotChanged;
	public static event Action<bool?>? IsPlotOwnerChanged;
	public static event Action<bool?>? IsPlotSpawnedChanged;

	private static void OnLocalChanged(GdPlayer? local)
	{
		var occupant = local is null ? null : GPlots.GetOccupant(local.Id);

		if (ReferenceEquals(_occupant, occupant))
			return;

		_occupant?.PlotChanged -= OnPlotChanged;
		_occupant = occupant;

		OnPlotChanged(occupant?.Plot);
		occupant?.PlotChanged += OnPlotChanged;
	}

	private static void OnPlotChanged(GdPlot? plot)
	{
		if (ReferenceEquals(_plot, plot))
			return;

		if (_plot is not null)
		{
			_plot.Occupants.OwnerChanged -= OnOwnerChanged;
			_plot.IsSpawnedChanged -= OnIsSpawnedChanged;
		}

		_plot = plot;

		Plot = plot;
		PlotChanged?.Invoke(plot);

		if (plot is null)
		{
			SetIsPlotOwner(null);
			SetIsPlotSpawned(null);
			return;
		}

		OnOwnerChanged(plot.Occupants.Owner);
		OnIsSpawnedChanged(plot.IsSpawned);

		plot.Occupants.OwnerChanged += OnOwnerChanged;
		plot.IsSpawnedChanged += OnIsSpawnedChanged;
	}

	private static void OnOwnerChanged(GdOccupant? owner) =>
		SetIsPlotOwner(owner is not null && ReferenceEquals(owner, _occupant));

	private static void OnIsSpawnedChanged(bool isSpawned) => SetIsPlotSpawned(isSpawned);

	private static void SetIsPlotOwner(bool? value)
	{
		if (IsPlotOwner == value)
			return;

		IsPlotOwner = value;
		IsPlotOwnerChanged?.Invoke(value);
	}

	private static void SetIsPlotSpawned(bool? value)
	{
		if (IsPlotSpawned == value)
			return;

		IsPlotSpawned = value;
		IsPlotSpawnedChanged?.Invoke(value);
	}
}
