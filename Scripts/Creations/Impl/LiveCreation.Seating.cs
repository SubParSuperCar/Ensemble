using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.Common.Input;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Scripts.Assets.Components;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.Scripts.Creations;

public partial class LiveCreation
{
	private readonly HashSet<Key> _boundKeys = [];
	private readonly System.Collections.Generic.Dictionary<int, Seat> _seatsByInstanceId = [];

	/// <summary>Whether someone sits in a seat with controls, the first thing a controller needs.</summary>
	internal bool IsControlSeatTaken
	{
		get
		{
			foreach (var seat in _seatsByInstanceId.Values)
				if (seat is { HasControls: true, Occupant: not null })
					return true;

			return false;
		}
	}

	/// <summary>The keys the creation's assets bind, the only ones worth sampling or sending.</summary>
	internal IReadOnlySet<Key> BoundKeys => _boundKeys;

	internal Seat? GetSeat(int instanceId) => _seatsByInstanceId.GetValueOrDefault(instanceId);

	/// <summary>Who sits where, as player IDs by instance ID, for a joining peer.</summary>
	internal Dictionary CaptureSeats()
	{
		var seats = new Dictionary();

		foreach (var (instanceId, seat) in _seatsByInstanceId)
			if (seat.Occupant is { } occupant)
				seats[instanceId] = occupant.Id;

		return seats;
	}

	internal void RestoreSeats(Dictionary seats)
	{
		foreach (var (instanceId, playerId) in seats)
			if (GetSeat(instanceId.AsInt32()) is { } seat)
				GPlayerManager.GetHandleOrNull(playerId.AsString())?.Sit(seat);
	}

	/// <summary>Stands every occupant up, as the creation is about to be freed.</summary>
	internal void VacateSeats()
	{
		foreach (var seat in _seatsByInstanceId.Values)
			seat.Occupant?.Vacate();
	}

	private void AddSeat(int instanceId, Seat? seat)
	{
		if (seat is not null)
			_seatsByInstanceId[instanceId] = seat;
	}

	private void AddBoundKeys(GdInstance instance)
	{
		foreach (var (name, value) in instance.Properties.Source.All)
			if (value.Type is CoreVariantType.Int64 && InputState.IsBinding(name))
				_boundKeys.Add((Key)(long)value);
	}

	private void VacateSeats(Assembly assembly)
	{
		// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
		foreach (var seat in _seatsByInstanceId.Values)
			if (seat.Handle.Assembly == assembly)
				seat.Occupant?.LeaveFallenSeat();
	}
}
