using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Plot;

namespace Root.Core.Gd.Plot;

[GlobalClass]
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

	// Safely getting this way is good, but is impossible in an instance like with IInstances (HoleyArray-based)
	public GdPlot? Get(int id) => _source.All.TryGetValue(id, out var plot) ? GdPlot.From(plot) : null;

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
			maxOccupantCount == Default ? null : maxOccupantCount,
			maxInstanceCount == Default ? null : maxInstanceCount));

	// I'd use the "Unlimited" sentinel here, but it's invalid because it doesn't represent no limit
	public void SetPlot(string playerId) => SetPlot(playerId, -1);

	public void SetPlot(string playerId, int plotId)
	{
		if (Guid.TryParse(playerId, out var guid))
			_source.SetPlot(guid, plotId == -1 ? null : plotId);
	}

	// TODO: Should this return null instead of throwing an exception when no value exists or the GUID cannot be parsed?
	// Answer: Yes, and the same for other getters under Gd as well (but it must be ensured that the call is safe)
	public GdOccupant GetOccupant(string playerId) => GdOccupant.From(_source.GetOccupant(Guid.Parse(playerId)));

	public void Lock() => _source.Lock();

	public Array<Dictionary> GetAllDicts()
	{
		var result = new Array<Dictionary>();

		foreach (var plot in _source.All.Values)
			result.Add(GdPlot.From(plot).ToDict());

		return result;
	}
}
