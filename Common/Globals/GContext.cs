using Root.GdCore.Players;
using Root.GdCore.Plots;

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

	public static GdPlot? LocalPlot { get; private set; }
	public static bool? IsPlotOwner { get; private set; }
	public static bool? IsLocalPlotSpawned { get; private set; }

	public static event Action<GdPlot?>? LocalPlotChanged;
	public static event Action<bool?>? IsPlotOwnerChanged;
	public static event Action<bool?>? IsLocalPlotSpawnedChanged;

	private static void OnLocalChanged(GdPlayer? local)
	{
		var occupant = local is null ? null : GPlots.GetOccupant(local.Id);

		if (ReferenceEquals(_occupant, occupant))
			return;

		_occupant?.PlotChanged -= OnLocalPlotChanged;
		_occupant = occupant;

		OnLocalPlotChanged(occupant?.Plot);
		occupant?.PlotChanged += OnLocalPlotChanged;
	}

	private static void OnLocalPlotChanged(GdPlot? plot)
	{
		if (ReferenceEquals(_plot, plot))
			return;

		if (_plot is not null)
		{
			_plot.Occupants.OwnerChanged -= OnOwnerChanged;
			_plot.IsSpawnedChanged -= OnIsLocalSpawnedChanged;
		}

		_plot = plot;

		LocalPlot = plot;
		LocalPlotChanged?.Invoke(plot);

		OnOwnerChanged(plot?.Occupants.Owner);
		SetIsLocalPlotSpawned(plot?.IsSpawned);

		if (plot is null)
			return;

		plot.Occupants.OwnerChanged += OnOwnerChanged;
		plot.IsSpawnedChanged += OnIsLocalSpawnedChanged;
	}

	private static void OnOwnerChanged(GdOccupant? owner) =>
		SetIsPlotOwner(owner is not null && ReferenceEquals(owner, _occupant));

	private static void OnIsLocalSpawnedChanged(bool isSpawned) => SetIsLocalPlotSpawned(isSpawned);

	private static void SetIsPlotOwner(bool? value)
	{
		if (IsPlotOwner == value)
			return;

		IsPlotOwner = value;
		IsPlotOwnerChanged?.Invoke(value);
	}

	private static void SetIsLocalPlotSpawned(bool? value)
	{
		if (IsLocalPlotSpawned == value)
			return;

		IsLocalPlotSpawned = value;
		IsLocalPlotSpawnedChanged?.Invoke(value);
	}
}
