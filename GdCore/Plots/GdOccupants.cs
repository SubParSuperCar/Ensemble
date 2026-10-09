using System.Runtime.CompilerServices;
using EnsembleCoreRoot.Api.Plots;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.GdCore.Plots;

/// <inheritdoc cref="IOccupants" />
public partial class GdOccupants : RefCounted
{
	[Signal]
	public delegate void AddedEventHandler(GdOccupant occupant);

	[Signal]
	public delegate void OwnerChangedEventHandler(GdOccupant? occupant);

	[Signal]
	public delegate void RemovedEventHandler(GdOccupant occupant);

	private static readonly ConditionalWeakTable<IOccupants, GdOccupants> Wrappers = [];

	/// <inheritdoc cref="IOccupants" />
	public IOccupants Source { get; private init; } = null!;

	public int Count => Source.All.Count;
	public int MaxCount => Source.MaxCount;

	public GdOccupant? Owner => Source.Owner is { } owner ? GdOccupant.From(owner) : null;

	public static GdOccupants From(IOccupants occupants) =>
		Wrappers.GetValue(occupants,
			static source =>
			{
				var wrapper = new GdOccupants { Source = source };

				source.Added += occupant => wrapper.EmitSignal(SignalName.Added, GdOccupant.From(occupant));
				source.Removed += occupant => wrapper.EmitSignal(SignalName.Removed, GdOccupant.From(occupant));

				source.OwnerChanged += occupant =>
					wrapper.EmitSignal(SignalName.OwnerChanged, (occupant is null ? null : GdOccupant.From(occupant))!);

				return wrapper;
			});

	public GdOccupant? GetOccupant(string playerId) =>
		Guid.TryParse(playerId, out var guid) && Source.All.TryGetValue(guid, out var occupant)
			? GdOccupant.From(occupant)
			: null;

	public Array<GdOccupant> GetAll() => [.. Source.All.Values.Select(GdOccupant.From)];

	public void SetOwner() => SetOwner(string.Empty);
	public void SetOwner(string playerId) => Source.SetOwner(Guid.TryParse(playerId, out var guid) ? guid : null);

	public void Clear() => Source.Clear();

	public Array<Dictionary> GetAllDicts() =>
		[.. Source.All.Values.Select(static occupant => GdOccupant.From(occupant).ToDict())];
}
