using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Plots;
using EnsembleRoot.GdCore.Players;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.GdCore.Plots;

/// <inheritdoc cref="IOccupant" />
public partial class GdOccupant : RefCounted
{
	[Signal]
	public delegate void PlotChangedEventHandler(GdPlot? plot);

	private static readonly ConditionalWeakTable<IOccupant, GdOccupant> Wrappers = [];

	/// <inheritdoc cref="IOccupant" />
	public IOccupant Source { get; private init; } = null!;

	/// <inheritdoc cref="GdPlayer" />
	public GdPlayer Player => GdPlayer.From(Source.Player);

	/// <inheritdoc cref="GdPlot" />
	public GdPlot? Plot => Source.Plot is { } plot ? GdPlot.From(plot) : null;

	public static GdOccupant From(IOccupant occupant) =>
		Wrappers.GetValue(occupant,
			static source =>
			{
				var wrapper = new GdOccupant { Source = source };

				source.PlotChanged += plot =>
					wrapper.EmitSignal(SignalName.PlotChanged, (plot is null ? null : GdPlot.From(plot))!);

				return wrapper;
			});

	public Dictionary ToDict() =>
		new()
		{
			["playerId"] = Player.Id,
			["plotId"] = Plot?.Id ?? None
		};

	public override string ToString() => Source.ToString()!;
}
