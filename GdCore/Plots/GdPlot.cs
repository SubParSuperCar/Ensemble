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

	/// <inheritdoc cref="IPlot" />
	public IPlot Source { get; private init; } = null!;

	public int Id => Source.Id;
	public bool IsSpawned => Source.IsSpawned;

	/// <inheritdoc cref="GdInstances" />
	public GdInstances Instances => field ??= GdInstances.From(Source.Instances);

	/// <inheritdoc cref="GdOccupants" />
	public GdOccupants Occupants => field ??= GdOccupants.From(Source.Occupants);

	public static GdPlot From(IPlot plot) =>
		Wrappers.GetValue(plot,
			static source =>
			{
				var wrapper = new GdPlot { Source = source };
				source.IsSpawnedChanged += isSpawned => wrapper.EmitSignal(SignalName.IsSpawnedChanged, isSpawned);

				return wrapper;
			});

	public void Spawn() => Source.Spawn();
	public void Despawn() => Source.Despawn();

	public void Reset() => Source.Reset();

	public Dictionary ToDict() =>
		new()
		{
			["id"] = Id,
			["isSpawned"] = IsSpawned
		};

	public override string ToString() => Source.ToString()!;
}
