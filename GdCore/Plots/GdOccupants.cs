using System.Runtime.CompilerServices;
using CoreRoot.Api.Plots;
using Godot;
using Godot.Collections;

namespace Root.GdCore.Plots;

public partial class GdOccupants : RefCounted
{
	[Signal]
	public delegate void AddedEventHandler(GdOccupant occupant);

	[Signal]
	public delegate void OwnerChangedEventHandler(GdOccupant? occupant);

	[Signal]
	public delegate void RemovedEventHandler(GdOccupant occupant);

	private static readonly ConditionalWeakTable<IOccupants, GdOccupants> Wrappers = [];

	private IOccupants _source = null!;

	public int Count => _source.All.Count;
	public int MaxCount => _source.MaxCount;

	public GdOccupant? Owner => _source.Owner is { } owner ? GdOccupant.From(owner) : null;

	public static GdOccupants From(IOccupants occupants) =>
		Wrappers.GetValue(occupants,
			static source =>
			{
				var wrapper = new GdOccupants { _source = source };

				source.Added += occupant => wrapper.EmitSignal(SignalName.Added, GdOccupant.From(occupant));
				source.Removed += occupant => wrapper.EmitSignal(SignalName.Removed, GdOccupant.From(occupant));

				source.OwnerChanged += occupant
					=> wrapper.EmitSignal(
						SignalName.OwnerChanged,
						(occupant is null ? null : GdOccupant.From(occupant))!);

				return wrapper;
			});

	public GdOccupant? GetOccupant(string playerId) =>
		Guid.TryParse(playerId, out var guid) && _source.All.TryGetValue(guid, out var occupant)
			? GdOccupant.From(occupant)
			: null;

	public Array<GdOccupant> GetAll()
	{
		var result = new Array<GdOccupant>();

		foreach (var occupant in _source.All.Values)
			result.Add(GdOccupant.From(occupant));

		return result;
	}

	public void SetOwner() => SetOwner(string.Empty);

	public void SetOwner(string playerId)
	{
		Guid? id = null;

		if (playerId != string.Empty && Guid.TryParse(playerId, out var guid))
			id = guid;

		_source.SetOwner(id);
	}

	public void Clear() => _source.Clear();

	public Array<Dictionary> GetAllDicts()
	{
		var result = new Array<Dictionary>();

		foreach (var occupant in _source.All.Values)
			result.Add(GdOccupant.From(occupant).ToDict());

		return result;
	}
}
