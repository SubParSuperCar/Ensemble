using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CoreRoot.Api.Plots;
using Godot;
using Godot.Collections;

namespace Root.GdCore.Plots;

public partial class GdPlots : RefCounted
{
	[Signal]
	public delegate void AddedEventHandler(GdPlot plot);

	[Signal]
	public delegate void RemovedEventHandler(GdPlot plot);

	private static readonly ConditionalWeakTable<IPlots, GdPlots> Wrappers = [];

	private IPlots _source = null!;

	public int Count => _source.All.Count;
	public bool IsLocked => _source.IsLocked;

	public static GdPlots From(IPlots plots) =>
		Wrappers.GetValue(plots,
			static source =>
			{
				var wrapper = new GdPlots { _source = source };

				source.Added += plot => wrapper.EmitSignal(SignalName.Added, GdPlot.From(plot));
				source.Removed += plot => wrapper.EmitSignal(SignalName.Removed, GdPlot.From(plot));

				return wrapper;
			});

	public GdPlot? GetPlot(int id) => _source.All.TryGetValue(id, out var plot) ? GdPlot.From(plot) : null;

	public Array<GdPlot> GetAll()
	{
		var result = new Array<GdPlot>();

		foreach (var plot in _source.All.Values)
			result.Add(GdPlot.From(plot));

		return result;
	}

	public GdPlot Add(int id) => Add(id, Default);
	public GdPlot Add(int id, int maxOccupantCount) => Add(id, maxOccupantCount, Default);

	public GdPlot Add(int id, int maxOccupantCount, int maxInstanceCount) =>
		GdPlot.From(_source.Add(
			id,
			maxOccupantCount is Default ? null : maxOccupantCount,
			maxInstanceCount is Default ? null : maxInstanceCount));

	public void SetPlot(string playerId) => SetPlot(playerId, None);
	public void SetPlot(string playerId, int plotId) => SetPlot(playerId, plotId, true);

	public void SetPlot(string playerId, int plotId, bool resolveOwnerIfNullOrRelinquishing) =>
		SetPlot(playerId, plotId, resolveOwnerIfNullOrRelinquishing, true);

	public void SetPlot(
		string playerId,
		int plotId,
		bool resolveOwnerIfNullOrRelinquishing,
		bool despawnAndClearInstancesIfLastToLeave)
	{
		if (!Guid.TryParse(playerId, out var guid))
			return;

		if (despawnAndClearInstancesIfLastToLeave)
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

		_source.SetPlot(guid, plotId is None ? null : plotId, resolveOwnerIfNullOrRelinquishing);
	}

	public GdOccupant? GetOccupant(string playerId) =>
		Guid.TryParse(playerId, out var guid) && TryGetOccupant(guid, out var occupant) ? occupant : null;

	public void Lock() => _source.Lock();

	public Array<Dictionary> GetAllDicts()
	{
		var result = new Array<Dictionary>();

		foreach (var plot in _source.All.Values)
			result.Add(GdPlot.From(plot).ToDict());

		return result;
	}

	private bool TryGetOccupant(Guid guid, [NotNullWhen(true)] out GdOccupant? occupant)
	{
		if (!_source.TryGetOccupant(guid, out var found))
		{
			occupant = null;
			return false;
		}

		occupant = GdOccupant.From(found);
		return true;
	}
}
