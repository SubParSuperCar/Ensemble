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

	private static readonly ConditionalWeakTable<IPlots, GdPlots> Cache = [];
	private IPlots _plots = null!;

	public int Count => _plots.All.Count;
	public bool IsLocked => _plots.IsLocked;

	public static GdPlots From(IPlots plots) => Cache.GetValue(plots,
		static p =>
		{
			var gdPlots = new GdPlots { _plots = p };
			p.Added += plot => gdPlots.EmitSignal(SignalName.Added, GdPlot.From(plot));

			return gdPlots;
		});

	public GdPlot? Get(int id)
		=> _plots.All.TryGetValue(id, out var plot) ? GdPlot.From(plot) : null;

	public Array<GdPlot> GetAll()
	{
		var plots = new Array<GdPlot>();

		foreach (var plot in _plots.All.Values)
			plots.Add(GdPlot.From(plot));

		return plots;
	}

	public GdPlot Add(int id) => Add(id, 0, 0);
	public GdPlot Add(int id, int maxOccupantCount) => Add(id, maxOccupantCount, 0);

	public GdPlot Add(int id, int maxOccupantCount, int maxInstanceCount)
		=> GdPlot.From(_plots.Add(
			id,
			maxOccupantCount == 0 ? null : maxOccupantCount,
			maxInstanceCount == 0 ? null : maxInstanceCount));

	public void SetPlot(string playerId) => SetPlot(playerId, -1);

	public void SetPlot(string playerId, int plotId)
	{
		if (Guid.TryParse(playerId, out var guid))
			_plots.SetPlot(guid, plotId == -1 ? null : plotId);
	}

	public GdOccupant GetOccupant(string playerId)
		=> GdOccupant.From(_plots.GetOccupant(Guid.Parse(playerId)));

	public void Lock() => _plots.Lock();
	public void Reset() => _plots.Reset();

	public Array<Dictionary> GetAllDicts()
	{
		var dicts = new Array<Dictionary>();

		foreach (var plot in _plots.All.Values)
			dicts.Add(GdPlot.From(plot).ToDict());

		return dicts;
	}
}
