using System.Diagnostics;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Scripts.Assets.Components;
using Godot;
using Serilog;

namespace EnsembleRoot.Scripts.Players;

public partial class PlayerHandle
{
	private static readonly StringName SeatLeaveAction = "character_seat_leave";

	// So leaving a seat doesn't sit right back down
	private static readonly TimeSpan SitCooldown = TimeSpan.FromSeconds(1);

	private long _leftSeatTimestamp;
	private (Vector3 Above, Vector3 Seat)? _standUp;
	private Vector3? _teleportAfterSeat;

	/// <summary>The seat the character sits in, which every peer follows on its own copy of the creation.</summary>
	public Seat? Seat { get; private set; }

	public bool CanSit => Seat is null && Stopwatch.GetElapsedTime(_leftSeatTimestamp) >= SitCooldown;

	/// <summary>
	///     Sits the character down, out of the physics space, so it can't disturb the creation or weigh on it.
	/// </summary>
	internal void Sit(Seat seat)
	{
		Vacate();

		Seat = seat;
		seat.Occupant = this;

		_seatVelocity = Vector3.Zero;
		_stateCount = 0;
		_standUp = null;
		_teleportAfterSeat = null;

		PhysicsServer3D.BodySetSpace(Body.GetRid(), default);
		Body.Velocity = Vector3.Zero;

		// Moved on physics ticks while seated, as the seat is, even on peers that otherwise move it every frame
		FollowSeat();
		Body.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Inherit;
		Body.ResetPhysicsInterpolation();

		_controller?.IsSeated = true;

		Log.Debug(
			"{Player} sat in instance {InstanceId} of plot {PlotId}",
			_player.Name, seat.Handle.InstanceId, seat.Handle.Plot.Id);
	}

	/// <summary>
	///     Takes the character out of its seat, if any. The local character stands up on its next physics tick; another
	///     peer's stays out of the space until its next state arrives, as it's still where the seat is.
	/// </summary>
	internal void Vacate()
	{
		if (Seat is not { } seat)
			return;

		Seat = null;
		seat.Occupant = null;

		_leftSeatTimestamp = Stopwatch.GetTimestamp();
		Body.GlobalRotation = new Vector3(0, Body.GlobalRotation.Y, 0);

		Log.Debug(
			"{Player} left instance {InstanceId} of plot {PlotId}",
			_player.Name, seat.Handle.InstanceId, seat.Handle.Plot.Id);

		if (_controller is null)
		{
			Body.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
			return;
		}

		if (_teleportAfterSeat is { } destination)
		{
			_teleportAfterSeat = null;
			Teleport(destination);
			return;
		}

		var position = Body.GlobalPosition;
		_standUp = (GetAbove(position, seat), position);
	}

	/// <summary>
	///     Respawns the local character out of a seat that fell out of the world, which would leave it falling too.
	///     Other peers' riders are their own peers' to move, as each peer's copy of the seat falls at its own moment.
	/// </summary>
	internal void LeaveFallenSeat()
	{
		if (_controller is not null)
			Respawn();
	}

	private void FollowSeat() => Body.GlobalTransform = Seat!.Area.GlobalTransform;

	private void UpdateSeatLeave()
	{
		if (Seat is not { Handle.Plot: var plot } || InputSink.IsSunk || !Input.IsActionJustPressed(SeatLeaveAction))
			return;

		// Space drives whatever binds it, while its controller sits; a reset still leaves the seat
		if (plot.GetController() != this || plot.Creation?.BoundKeys.Contains(Key.Space) is not true)
			Seat.RequestLeave();
	}

	// Clear of the seat's whole assembly, so the cast down starts in the open
	private Vector3 GetAbove(Vector3 position, Seat seat)
	{
		var assembly = seat.Handle.Assembly!;
		var top = (assembly.GlobalTransform * assembly.Bounds.GetCenter()).Y + assembly.Bounds.Size.Length() / 2;

		return position with { Y = Math.Max(top, position.Y) + BodyBounds.Size.Y };
	}

	// The engine finds where the character fits, against whatever is really there: it lands on top of what's above the
	// seat, or back at the seat if nothing is
	private void StandUp(Vector3 above, Vector3 seat)
	{
		Jump(above);

		var collision = new KinematicCollision3D();
		var isBlocked = Body.TestMove(Body.GlobalTransform, seat - above, collision);

		Jump(isBlocked ? above + collision.GetTravel() : seat);
	}
}
