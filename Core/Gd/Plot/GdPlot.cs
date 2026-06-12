using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Plot;
using Root.Core.Gd.Asset;

namespace Root.Core.Gd.Plot;

[GlobalClass]
public partial class GdPlot : RefCounted
{
	[Signal]
	public delegate void IsSpawnedChangedEventHandler(bool isSpawned);

	private static readonly ConditionalWeakTable<IPlot, GdPlot> Cache = [];
	private IPlot _plot = null!;

	public int Id => _plot.Id;
	public bool IsSpawned => _plot.IsSpawned;

	public GdInstances Instances => field ??= GdInstances.From(_plot.Instances);
	public GdOccupants Occupants => field ??= GdOccupants.From(_plot.Occupants);

	public static GdPlot From(IPlot plot) => Cache.GetValue(plot,
		static p =>
		{
			var gdPlot = new GdPlot { _plot = p };
			p.IsSpawnedChanged += isSpawned => gdPlot.EmitSignal(SignalName.IsSpawnedChanged, isSpawned);

			return gdPlot;
		});

	public void Spawn() => _plot.Spawn();
	public void Despawn() => _plot.Despawn();

	public void Reset() => _plot.Reset();

	public Dictionary ToDict()
		=> new()
		{
			["id"] = Id,
			["isSpawned"] = IsSpawned
		};

	public override string ToString() => _plot.ToString()!;
}
