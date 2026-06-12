using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Plot;
using Root.Core.Gd.Player;

namespace Root.Core.Gd.Plot;

[GlobalClass]
public partial class GdOccupant : RefCounted
{
	[Signal]
	public delegate void PlotChangedEventHandler(GdPlot? plot);

	private static readonly ConditionalWeakTable<IOccupant, GdOccupant> Cache = [];
	private IOccupant _occupant = null!;

	public GdPlayer Player => GdPlayer.From(_occupant.Player);
	public GdPlot? Plot => _occupant.Plot is { } plot ? GdPlot.From(plot) : null;

	public static GdOccupant From(IOccupant occupant) => Cache.GetValue(occupant,
		static o =>
		{
			var gdOccupant = new GdOccupant { _occupant = o };

			o.PlotChanged += plot
				=> gdOccupant.EmitSignal(SignalName.PlotChanged, (plot is null ? null : GdPlot.From(plot))!);

			return gdOccupant;
		});

	public Dictionary ToDict()
		=> new()
		{
			["playerId"] = Player.Id,
			["plotId"] = Plot?.Id ?? -1
		};

	public override string ToString() => _occupant.ToString()!;
}
