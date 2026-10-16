using EnsembleRoot.GdCore.Players;
using EnsembleRoot.GdCore.Plots;
using Godot;

namespace EnsembleRoot.Globals;

/// <remarks>
///     The properties are always current, but Core passes through intermediate states (e.g., no owner while a player
///     switches plots), so the events are coalesced and raised once per frame, deferred, with the settled values.
/// </remarks>
public static class GContext
{
	private static GdOccupant? _occupant;

	private static bool _isNotifyQueued;
	private static GdPlot? _notifiedLocalPlot;
	private static bool? _notifiedIsPlotOwner;
	private static bool? _notifiedIsLocalPlotSpawned;

	static GContext()
	{
		// Throws unless GdCore is initialized before GContext's first use
		OnLocalChanged(GPlayers.Local);
		GPlayers.LocalChanged += OnLocalChanged;

		_notifiedLocalPlot = LocalPlot;
		_notifiedIsPlotOwner = IsPlotOwner;
		_notifiedIsLocalPlotSpawned = IsLocalPlotSpawned;
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
		QueueNotify();

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
		QueueNotify();
	}

	private static void SetIsLocalPlotSpawned(bool? value)
	{
		if (IsLocalPlotSpawned == value)
			return;

		IsLocalPlotSpawned = value;
		QueueNotify();
	}

	private static void QueueNotify()
	{
		if (_isNotifyQueued)
			return;

		_isNotifyQueued = true;
		Callable.From(Notify).CallDeferred();
	}

	private static void Notify()
	{
		_isNotifyQueued = false;

		if (!ReferenceEquals(_notifiedLocalPlot, LocalPlot))
		{
			_notifiedLocalPlot = LocalPlot;
			LocalPlotChanged?.Invoke(LocalPlot);
		}

		if (_notifiedIsPlotOwner != IsPlotOwner)
		{
			_notifiedIsPlotOwner = IsPlotOwner;
			IsPlotOwnerChanged?.Invoke(IsPlotOwner);
		}

		// ReSharper disable once InvertIf
		if (_notifiedIsLocalPlotSpawned != IsLocalPlotSpawned)
		{
			_notifiedIsLocalPlotSpawned = IsLocalPlotSpawned;
			IsLocalPlotSpawnedChanged?.Invoke(IsLocalPlotSpawned);
		}
	}
}
