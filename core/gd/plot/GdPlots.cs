using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Plot;

namespace Root.Core.Gd.Plot;

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

	public void SetPlot(string playerId, int plotId)
	{
		if (Guid.TryParse(playerId, out var guid))
			_source.SetPlot(guid, plotId is None ? null : plotId);
	}

	// I'd like to add this new overload that'll become the main method for setting plots via the wrapper.
	// The problem is that internally, performing these exhange ops individually causes there to be a time where
	// events are fired that makes it look like another operation is running. For example, attempting to set a plot
	// to another causes there to be a brief moment where PlotChanged is emitted with null, even though it shouldn't.
	// This is a bad setup that is logically flawed and could be prevented by maybe adding additional args for skipping
	// event firing, etc., or dedicated exhange methods. I'd like to keep it as lightweight as possible, though.
	//
	// My goal here is to make the wrapper is responsible for complex ops like resolving Owner instead of the inner Core.
	// Right now, you have to access Core to disable automatic owner setting, and also there's no capability for
	// resetting the plot when the last occupent leaves, which is what I'm trying to accomplish here. All together,
	// the Core should just feature the correct logic and we need to dig through other parts of the code to ensure
	// that there are no logical flaws as well, mostly exhange issues. When switching plots, we don't want a moment where
	// it's null, we just want from one plot to another, and everything should happen in the right order.
	//
	// For the reset op, the reset call should probably occur before the owner relinquishes, because they lose authority
	// then.
	//
	// This method is W.I.P.
	// We should probably trace the logic flow of every possible case and make sure it's 100% logical across
	// the entire Core system, not just Plots/Occupants but Players and Instances too, just everything.
	public void SetPlot(
		string playerId, int plotId,
		bool setOwnerIfFirstToJoinOrRelinquishing = true, // If we're the first to join or are the owner leaving, update owner accordingly
		bool resetIfLastToLeave = true) // If we're the last to leave, reset the plot (despawn, clear instances, etc.)
	{
		if (Guid.TryParse(playerId, out var guid))
			_source.SetPlot(guid, plotId is None ? null : plotId);
	}

	public GdOccupant? GetOccupant(string playerId)
	{
		if (!Guid.TryParse(playerId, out var guid))
			return null;

		try
		{
			return GdOccupant.From(_source.GetOccupant(guid));
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	public void Lock() => _source.Lock();

	public Array<Dictionary> GetAllDicts()
	{
		var result = new Array<Dictionary>();

		foreach (var plot in _source.All.Values)
			result.Add(GdPlot.From(plot).ToDict());

		return result;
	}
}
