using EnsembleRoot.GdCore.Players;
using EnsembleRoot.GdCore.Plots;

namespace EnsembleRoot.Globals;

public static class GContext
{
	private static GdOccupant? _occupant;

	static GContext()
	{
		// GdCore must be initialized before GContext can be used; otherwise, this will fail
		OnLocalChanged(GPlayers.Local);
		GPlayers.LocalChanged += OnLocalChanged;
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
		OnOwnerChanged(LocalPlot?.Occupants.Owner);
		occupant?.PlotChanged += OnLocalPlotChanged;
	}

	private static void OnLocalPlotChanged(GdPlot? plot)
	{
		if (ReferenceEquals(LocalPlot, plot))
			return;

		if (LocalPlot is not null)
		{
			LocalPlot.Occupants.OwnerChanged -= OnOwnerChanged;
			LocalPlot.IsSpawnedChanged -= OnIsLocalPlotSpawnedChanged;
		}

		LocalPlot = plot;
		LocalPlotChanged?.Invoke(plot);

		OnOwnerChanged(plot?.Occupants.Owner);
		SetIsLocalPlotSpawned(plot?.IsSpawned);

		if (plot is null)
			return;

		plot.Occupants.OwnerChanged += OnOwnerChanged;
		plot.IsSpawnedChanged += OnIsLocalPlotSpawnedChanged;
	}

	private static void OnOwnerChanged(GdOccupant? owner) =>
		SetIsPlotOwner(LocalPlot is null ? null : ReferenceEquals(owner, _occupant));

	private static void OnIsLocalPlotSpawnedChanged(bool isSpawned) => SetIsLocalPlotSpawned(isSpawned);

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
