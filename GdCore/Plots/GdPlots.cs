using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Plots;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.GdCore.Plots;

/// <inheritdoc cref="IPlots" />
public partial class GdPlots : RefCounted
{
	[Signal]
	public delegate void AddedEventHandler(GdPlot plot);

	[Signal]
	public delegate void RemovedEventHandler(GdPlot plot);

	private static readonly ConditionalWeakTable<IPlots, GdPlots> Wrappers = [];

	/// <inheritdoc cref="IPlots" />
	public IPlots Source { get; private init; } = null!;

	public int Count => Source.All.Count;
	public bool IsLocked => Source.IsLocked;

	public static GdPlots From(IPlots plots) =>
		Wrappers.GetValue(plots,
			static source =>
			{
				var wrapper = new GdPlots { Source = source };

				source.Added += plot => wrapper.EmitSignal(SignalName.Added, GdPlot.From(plot));
				source.Removed += plot => wrapper.EmitSignal(SignalName.Removed, GdPlot.From(plot));

				return wrapper;
			});

	public GdPlot? GetPlot(int id) => Source.All.TryGetValue(id, out var plot) ? GdPlot.From(plot) : null;

	public Array<GdPlot> GetAll()
	{
		var result = new Array<GdPlot>();

		foreach (var plot in Source.All.Values)
			result.Add(GdPlot.From(plot));

		return result;
	}

	public GdPlot Add(int id) => Add(id, Default);
	public GdPlot Add(int id, int maxOccupantCount) => Add(id, maxOccupantCount, Default);

	public GdPlot Add(int id, int maxOccupantCount, int maxInstanceCount) =>
		GdPlot.From(Source.Add(
			id,
			maxOccupantCount is Default ? null : maxOccupantCount,
			maxInstanceCount is Default ? null : maxInstanceCount));

	public void SetPlot(string playerId) => SetPlot(playerId, None);
	public void SetPlot(string playerId, int plotId) => SetPlot(playerId, plotId, true);

	public void SetPlot(string playerId, int plotId, bool shouldResolveOwnerIfNullOrRelinquishing) =>
		SetPlot(playerId, plotId, shouldResolveOwnerIfNullOrRelinquishing, true);

	public void SetPlot(
		string playerId,
		int plotId,
		bool shouldResolveOwnerIfNullOrRelinquishing,
		bool shouldDespawnAndClearInstancesIfLastToLeave)
	{
		if (!Guid.TryParse(playerId, out var guid) || (plotId is not None && !Source.All.ContainsKey(plotId)))
			return;

		if (shouldDespawnAndClearInstancesIfLastToLeave)
		{
			if (!TryGetOccupant(guid, out var occupant))
				return;

			if (occupant.Plot is { Occupants.Count: < 2 } current)
			{
				if (current.Id == plotId)
					return;

				current.Despawn();
				current.Instances.Clear();
			}
		}

		Source.SetPlot(guid, plotId is None ? null : plotId, shouldResolveOwnerIfNullOrRelinquishing);
	}

	public GdOccupant? GetOccupant(string playerId) =>
		Guid.TryParse(playerId, out var guid) && TryGetOccupant(guid, out var occupant) ? occupant : null;

	public void Lock() => Source.Lock();

	public Array<Dictionary> GetAllDicts()
	{
		var result = new Array<Dictionary>();

		foreach (var plot in Source.All.Values)
			result.Add(GdPlot.From(plot).ToDict());

		return result;
	}

	private bool TryGetOccupant(Guid guid, [NotNullWhen(true)] out GdOccupant? occupant)
	{
		if (!Source.TryGetOccupant(guid, out var found))
		{
			occupant = null;
			return false;
		}

		occupant = GdOccupant.From(found);
		return true;
	}
}
