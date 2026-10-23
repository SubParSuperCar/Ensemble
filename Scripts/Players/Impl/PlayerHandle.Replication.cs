using System.Diagnostics;
using System.Runtime.InteropServices;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Sessions;
using EnsembleRoot.Sessions.Api;
using Godot;

namespace EnsembleRoot.Scripts.Players;

public partial class PlayerHandle
{
	private const int ReplicationChannel = 1;
	private const int ReplicationRate = 20;

	// Below these, a character counts as idle and only sends keepalives, once a second, which also recover from lost
	// packets
	private const float MinReplicatedDistance = 1 / 128f;
	private const float MinReplicatedYaw = 1 / 128f;
	private const int IdleReplicationStates = ReplicationRate;

	// Others' characters are shown this far in the past, so a late state still arrives before it's needed
	private const double InterpolationDelay = 2d / ReplicationRate;
	private const int StateBufferSize = 4;
	private const float SnapDistance = 8f;

	// Bounds what the host relays, since every update goes out to all other peers
	private const double MinRelayInterval = 1d / ReplicationRate / 2;
	private const float MaxReplicatedCoordinate = 64 * 1024f;

	// Whole physics ticks, so each sequence number stands for an exact moment on the sender
	private static readonly ulong ReplicationTicks = (ulong)Math.Max(1, Engine.PhysicsTicksPerSecond / ReplicationRate);
	private static readonly double ReplicationInterval = ReplicationTicks / (double)Engine.PhysicsTicksPerSecond;

	private readonly CharacterState[] _states = new CharacterState[StateBufferSize];
	private readonly StreamTimeline _timeline = new(ReplicationInterval);

	private double _clock;
	private long _lastRelayTimestamp;
	private Vector3 _lastReplicatedPosition;
	private ushort _lastReplicatedSequence;
	private float _lastReplicatedYaw;
	private Vector3 _shownPosition;
	private float _shownYaw;
	private int _stateCount;

	private static bool IsReplicating => GSessionManager is { Mode: SessionMode.MultiPlayer, IsActive: true };

	/// <summary>The local character's state, or the latest one received for another's (if any).</summary>
	public (Vector3 Position, float Yaw)? ReplicatedState =>
		Controller is not null ? (Body.GlobalPosition, Body.GlobalRotation.Y) :
		_stateCount > 0 ? (_states[_stateCount - 1].Position, _states[_stateCount - 1].Yaw) : null;

	/// <summary>Places another's character from a snapshot, unless an update already did.</summary>
	public void RestoreReplicatedState(Vector3 position, float yaw)
	{
		if (Controller is not null || _stateCount > 0 || !IsValidState(position, yaw))
			return;

		// Held until live states arrive, which start the timeline
		Show(position, yaw, true);
		_states[0] = new CharacterState(double.NegativeInfinity, position, yaw);
		_stateCount = 1;
	}

	private void Replicate()
	{
		var frame = Engine.GetPhysicsFrames();

		if (!IsReplicating || frame % ReplicationTicks is not 0)
			return;

		var sequence = (ushort)(frame / ReplicationTicks);
		var position = Body.GlobalPosition;
		var yaw = Body.GlobalRotation.Y;

		var isIdle =
			position.DistanceTo(_lastReplicatedPosition) < MinReplicatedDistance &&
			MathF.Abs(Mathf.AngleDifference(_lastReplicatedYaw, yaw)) < MinReplicatedYaw;

		if (isIdle && (ushort)(sequence - _lastReplicatedSequence) < IdleReplicationStates)
			return;

		_lastReplicatedSequence = sequence;
		_lastReplicatedPosition = position;
		_lastReplicatedYaw = yaw;

		if (GSessionManager.IsServer)
			Relay(sequence, position, yaw, GSessionManager.LocalPeerId);
		else
			RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcReplicate, sequence, position, yaw);
	}

	// Between the buffered states around the moment shown, held at the latest one when the next is late
	private void Interpolate(double delta)
	{
		_clock += delta;

		if (_stateCount is 0)
			return;

		var time = _clock - InterpolationDelay;
		var index = _stateCount - 1;

		while (index >= 0 && _states[index].Time > time)
			index--;

		if (index < 0 || index == _stateCount - 1)
		{
			ref readonly var held = ref _states[Math.Max(index, 0)];
			Show(held.Position, held.Yaw, false);

			return;
		}

		ref readonly var from = ref _states[index];
		ref readonly var to = ref _states[index + 1];
		var t = (float)((time - from.Time) / (to.Time - from.Time));

		Show(from.Position.Lerp(to.Position, t), Mathf.LerpAngle(from.Yaw, to.Yaw, t), false);
	}

	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered,
		TransferChannel = ReplicationChannel)]
	private void RpcReplicate(ushort sequence, Vector3 position, float yaw)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		NetworkConditions.Receive(this, () => ReceiveReplicated(senderId, sequence, position, yaw));
	}

	private void ReceiveReplicated(int senderId, ushort sequence, Vector3 position, float yaw)
	{
		if (!IsValidState(position, yaw))
			return;

		if (GSessionManager.IsServer)
		{
			if (
				GSessionManager.GetPeerByPlayerId(Id)?.Id != senderId ||
				Stopwatch.GetElapsedTime(_lastRelayTimestamp).TotalSeconds < MinRelayInterval)
				return;

			_lastRelayTimestamp = Stopwatch.GetTimestamp();
			Relay(sequence, position, yaw, senderId);
		}
		else if (senderId != MultiplayerPeer.TargetPeerServer)
			return;

		AddState(_timeline.GetTime(sequence, _clock), position, yaw);
	}

	// Also false for NaN
	private static bool IsValidState(Vector3 position, float yaw) =>
		float.IsFinite(yaw) &&
		position.Abs() is
		{
			X: <= MaxReplicatedCoordinate, Y: <= MaxReplicatedCoordinate, Z: <= MaxReplicatedCoordinate
		};

	// The first state is jumped to, rather than interpolated toward from the spawn location
	private void AddState(double time, Vector3 position, float yaw)
	{
		if (_stateCount is 0)
			Show(position, yaw, true);
		else
		{
			ref var latest = ref _states[_stateCount - 1];

			if (time <= latest.Time)
				return;

			// A gap means the character idled, so it holds its pose until an interval before it moves again
			if (time - latest.Time > 2 * ReplicationInterval)
				latest = latest with { Time = time - ReplicationInterval };
		}

		if (_stateCount == StateBufferSize)
			Array.Copy(_states, 1, _states, 0, --_stateCount);

		_states[_stateCount++] = new CharacterState(time, position, yaw);
	}

	// Remembers what it last showed, so a character standing still costs no interop
	private void Show(Vector3 position, float yaw, bool isJump)
	{
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if (!isJump && position == _shownPosition && yaw == _shownYaw)
			return;

		var body = Body;

		if (isJump || position.DistanceTo(_shownPosition) > SnapDistance)
			Jump(position);
		else
			body.GlobalPosition = position;

		body.GlobalRotation = body.GlobalRotation with { Y = yaw };

		_shownPosition = position;
		_shownYaw = yaw;
	}

	private void Relay(ushort sequence, Vector3 position, float yaw, int sourcePeerId)
	{
		foreach (var peerId in GSessionManager.Peers.Keys)
			if (peerId != sourcePeerId && GSessionManager.IsPeerSynced(peerId))
				RpcId(peerId, MethodName.RpcReplicate, sequence, position, yaw);
	}

	[StructLayout(LayoutKind.Auto)]
	private readonly record struct CharacterState(double Time, Vector3 Position, float Yaw);
}
