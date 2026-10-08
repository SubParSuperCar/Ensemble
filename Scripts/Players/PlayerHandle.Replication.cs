using EnsembleRoot.Common.Utils;
using EnsembleRoot.SessionManager.Api;
using Godot;

namespace EnsembleRoot.Scripts.Players;

public partial class PlayerHandle
{
	private const int ReplicationChannel = 1;
	private const double ReplicationInterval = 1 / 20d;

	private const float SmoothingRate = 16f;
	private const float SnapDistance = 8f;

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

		_sinceLastReplication = 0;

		var position = Body.GlobalPosition;
		var yaw = Body.GlobalRotation.Y;

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
		if (!position.IsFinite() || !float.IsFinite(yaw))
			return;

		var senderId = Multiplayer.GetRemoteSenderId();

		if (GSessionManager.IsServer)
		{
			if (GSessionManager.GetPeerByPlayerId(Id)?.Id != senderId)
				return;

			Relay(position, yaw, senderId);
		}
		else if (senderId != MultiplayerPeer.TargetPeerServer)
			return;

		_targetPosition = position;
		_targetYaw = yaw;
	}

	private void Relay(Vector3 position, float yaw, int sourcePeerId)
	{
		foreach (var peerId in GSessionManager.Peers.Keys)
			if (peerId != sourcePeerId && GSessionManager.IsPeerSynced(peerId))
				RpcId(peerId, MethodName.RpcReplicate, position, yaw);
	}
}
