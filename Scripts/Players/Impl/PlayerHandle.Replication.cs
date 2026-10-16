using System.Diagnostics;
using EnsembleRoot.Common.Utils;
using EnsembleRoot.Sessions.Api;
using Godot;

namespace EnsembleRoot.Scripts.Players;

public partial class PlayerHandle
{
	private const int ReplicationChannel = 1;
	private const double ReplicationInterval = 1 / 20d;
	private const double IdleReplicationInterval = 1d;

	// Below these, a character counts as idle and only sends keepalives, which also recover from lost packets
	private const float MinReplicatedDistance = 1 / 128f;
	private const float MinReplicatedYaw = 1 / 128f;

	private const float SmoothingRate = 16f;
	private const float SnapDistance = 8f;

	// Bounds what the host relays, since every update goes out to all other peers
	private const double MinRelayInterval = ReplicationInterval / 2;
	private const float MaxReplicatedCoordinate = 64 * 1024f;

	private long _lastRelayTimestamp;
	private Vector3 _lastReplicatedPosition;
	private float _lastReplicatedYaw;
	private double _sinceLastReplication;
	private Vector3? _targetPosition;
	private float _targetYaw;

	private static bool IsReplicating => GSessionManager is { Mode: SessionMode.MultiPlayer, IsActive: true };

	private void Replicate(double delta)
	{
		if (!IsReplicating)
			return;

		_sinceLastReplication += delta;
		if (_sinceLastReplication < ReplicationInterval)
			return;

		var position = Body.GlobalPosition;
		var yaw = Body.GlobalRotation.Y;

		var isIdle =
			position.DistanceTo(_lastReplicatedPosition) < MinReplicatedDistance &&
			MathF.Abs(Mathf.AngleDifference(_lastReplicatedYaw, yaw)) < MinReplicatedYaw;

		if (isIdle && _sinceLastReplication < IdleReplicationInterval)
			return;

		_sinceLastReplication = 0;
		_lastReplicatedPosition = position;
		_lastReplicatedYaw = yaw;

		if (GSessionManager.IsServer)
			Relay(position, yaw, GSessionManager.LocalPeerId);
		else
			RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcReplicate, position, yaw);
	}

	private void Interpolate(double delta)
	{
		if (_targetPosition is not { } target)
			return;

		var body = Body;
		var position = body.GlobalPosition;
		var weight = Smoothing.GetWeight(SmoothingRate, delta);

		body.GlobalPosition = position.DistanceTo(target) > SnapDistance ? target : position.Lerp(target, weight);

		var rotation = body.GlobalRotation;
		rotation.Y = Mathf.LerpAngle(rotation.Y, _targetYaw, weight);

		body.GlobalRotation = rotation;
	}

	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered,
		TransferChannel = ReplicationChannel)]
	private void RpcReplicate(Vector3 position, float yaw)
	{
		if (!IsWithinWorld(position) || !float.IsFinite(yaw))
			return;

		var senderId = Multiplayer.GetRemoteSenderId();

		if (GSessionManager.IsServer)
		{
			if (
				GSessionManager.GetPeerByPlayerId(Id)?.Id != senderId ||
				Stopwatch.GetElapsedTime(_lastRelayTimestamp).TotalSeconds < MinRelayInterval)
				return;

			_lastRelayTimestamp = Stopwatch.GetTimestamp();
			Relay(position, yaw, senderId);
		}
		else if (senderId != MultiplayerPeer.TargetPeerServer)
			return;

		if (_targetPosition is null)
			Reveal(position, yaw);

		_targetPosition = position;
		_targetYaw = yaw;
	}

	private void Reveal(Vector3 position, float yaw)
	{
		var body = Body;

		body.GlobalPosition = position;
		body.GlobalRotation = body.GlobalRotation with { Y = yaw };
		body.ResetPhysicsInterpolation();

		body.ProcessMode = ProcessModeEnum.Inherit;
		body.Visible = true;
	}

	// Also false for NaN
	private static bool IsWithinWorld(Vector3 position) =>
		position.Abs() is
		{
			X: <= MaxReplicatedCoordinate, Y: <= MaxReplicatedCoordinate, Z: <= MaxReplicatedCoordinate
		};

	private void Relay(Vector3 position, float yaw, int sourcePeerId)
	{
		foreach (var peerId in GSessionManager.Peers.Keys)
			if (peerId != sourcePeerId && GSessionManager.IsPeerSynced(peerId))
				RpcId(peerId, MethodName.RpcReplicate, position, yaw);
	}
}
