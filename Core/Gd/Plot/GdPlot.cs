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

	private static readonly ConditionalWeakTable<IPlot, GdPlot> Wrappers = [];
	private IPlot _source = null!;

	public int Id => _source.Id;
	public bool IsSpawned => _source.IsSpawned;

	public GdInstances Instances => field ??= GdInstances.From(_source.Instances);
	public GdOccupants Occupants => field ??= GdOccupants.From(_source.Occupants);

	public static GdPlot From(IPlot plot) => Wrappers.GetValue(plot,
		static source =>
		{
			var wrapper = new GdPlot { _source = source };
			source.IsSpawnedChanged += isSpawned => wrapper.EmitSignal(SignalName.IsSpawnedChanged, isSpawned);

			return wrapper;
		});

	public void Spawn() => _source.Spawn();
	public void Despawn() => _source.Despawn();

	public void Reset() => _source.Reset();

	public Dictionary ToDict() => new()
	{
		["id"] = Id,
		["isSpawned"] = IsSpawned
	};

	public override string ToString() => _source.ToString()!;
}
