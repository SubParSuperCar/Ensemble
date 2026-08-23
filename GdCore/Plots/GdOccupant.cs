using System.Runtime.CompilerServices;
using CoreRoot.Api.Plots;
using Godot;
using Godot.Collections;
using Root.GdCore.Players;

namespace Root.GdCore.Plots;

public partial class GdOccupant : RefCounted
{
	[Signal]
	public delegate void PlotChangedEventHandler(GdPlot? plot);

	private static readonly ConditionalWeakTable<IOccupant, GdOccupant> Wrappers = [];
	private IOccupant _source = null!;

	public GdPlayer Player => GdPlayer.From(_source.Player);
	public GdPlot? Plot => _source.Plot is { } plot ? GdPlot.From(plot) : null;

	public static GdOccupant From(IOccupant occupant) =>
		Wrappers.GetValue(occupant,
			static source =>
			{
				var wrapper = new GdOccupant { _source = source };

				source.PlotChanged += plot
					=> wrapper.EmitSignal(SignalName.PlotChanged, (plot is null ? null : GdPlot.From(plot))!);

				return wrapper;
			});

	public Dictionary ToDict() =>
		new()
		{
			["playerId"] = Player.Id,
			["plotId"] = Plot?.Id ?? None
		};

	public override string ToString() => _source.ToString()!;
}
