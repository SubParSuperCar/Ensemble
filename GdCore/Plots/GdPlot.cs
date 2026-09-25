using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Plots;
using EnsembleRoot.GdCore.Assets;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.GdCore.Plots;

/// <inheritdoc cref="IPlot" />
public partial class GdPlot : RefCounted
{
	[Signal]
	public delegate void IsSpawnedChangedEventHandler(bool isSpawned);

	private static readonly ConditionalWeakTable<IPlot, GdPlot> Wrappers = [];

	private IPlot _source = null!;

	public int Id => _source.Id;
	public bool IsSpawned => _source.IsSpawned;

	/// <inheritdoc cref="GdInstances" />
	public GdInstances Instances => field ??= GdInstances.From(_source.Instances);

	/// <inheritdoc cref="GdOccupants" />
	public GdOccupants Occupants => field ??= GdOccupants.From(_source.Occupants);

	public static GdPlot From(IPlot plot) =>
		Wrappers.GetValue(plot,
			static source =>
			{
				var wrapper = new GdPlot { _source = source };
				source.IsSpawnedChanged += isSpawned => wrapper.EmitSignal(SignalName.IsSpawnedChanged, isSpawned);

				return wrapper;
			});

	public void Spawn() => _source.Spawn();
	public void Despawn() => _source.Despawn();

	public void Reset() => _source.Reset();

	public Dictionary ToDict() =>
		new()
		{
			["id"] = Id,
			["isSpawned"] = IsSpawned
		};

	public override string ToString() => _source.ToString()!;
}
