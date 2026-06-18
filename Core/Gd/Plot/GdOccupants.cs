using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Root.Core.Api.Plot;

namespace Root.Core.Gd.Plot;

[GlobalClass]
public partial class GdOccupants : RefCounted
{
	[Signal]
	public delegate void AddedEventHandler(GdOccupant occupant);

	[Signal]
	public delegate void OwnerChangedEventHandler(GdOccupant? occupant);

	[Signal]
	public delegate void RemovedEventHandler(GdOccupant occupant);

	private static readonly ConditionalWeakTable<IOccupants, GdOccupants> Cache = [];
	private IOccupants _occupants = null!;

	public int Count => _occupants.All.Count;
	public int MaxCount => _occupants.MaxCount;

	public GdOccupant? Owner => _occupants.Owner is { } owner ? GdOccupant.From(owner) : null;

	public static GdOccupants From(IOccupants occupants) => Cache.GetValue(occupants,
		static value =>
		{
			var wrapper = new GdOccupants { _occupants = value };

			value.Added += occupant => wrapper.EmitSignal(SignalName.Added, GdOccupant.From(occupant));
			value.Removed += occupant => wrapper.EmitSignal(SignalName.Removed, GdOccupant.From(occupant));

			value.OwnerChanged += occupant
				=> wrapper.EmitSignal(
					SignalName.OwnerChanged,
					(occupant is null ? null : GdOccupant.From(occupant))!);

			return wrapper;
		});

	public GdOccupant? Get(string playerId)
		=> Guid.TryParse(playerId, out var guid) && _occupants.All.TryGetValue(guid, out var occupant)
			? GdOccupant.From(occupant)
			: null;

	public Array<GdOccupant> GetAll()
	{
		var result = new Array<GdOccupant>();

		foreach (var occupant in _occupants.All.Values)
			result.Add(GdOccupant.From(occupant));

		return result;
	}

	public void SetOwner() => SetOwner(string.Empty);

	public void SetOwner(string playerId)
	{
		Guid? id = null;

		if (playerId != string.Empty && Guid.TryParse(playerId, out var guid))
			id = guid;

		_occupants.SetOwner(id);
	}

	public void Clear() => _occupants.Clear();

	public Array<Dictionary> GetAllDicts()
	{
		var result = new Array<Dictionary>();

		foreach (var occupant in _occupants.All.Values)
			result.Add(GdOccupant.From(occupant).ToDict());

		return result;
	}
}
