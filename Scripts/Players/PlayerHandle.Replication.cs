using EnsembleRoot.SessionManager.Api;
using Godot;

namespace EnsembleRoot.Scripts.Players;

public partial class PlayerHandle
{
	private const int ReplicationChannel = 1;
	private const double ReplicationInterval = 1 / 20d;

	private const float SmoothingRate = 16;
	private const float SnapDistance = 8;

	private double _sinceLastReplication;
	private Vector3? _targetPosition;
	private float _targetYaw;

	private static bool IsReplicating => GSessionManager is { Mode: SessionMode.MultiPlayer, IsActive: true };

	public override void _PhysicsProcess(double delta)
	{
		if (Controller is null || !IsReplicating)
			return;

		_sinceLastReplication += delta;
		if (_sinceLastReplication < ReplicationInterval)
			return;

		_sinceLastReplication = 0;

		var position = Controller.GlobalPosition;
		var yaw = Controller.GlobalRotation.Y;

		if (GSessionManager.IsServer)
			Relay(position, yaw, GSessionManager.LocalPeerId);
		else
			RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcReplicate, position, yaw);
	}

	public override void _Process(double delta)
	{
		if (Controller is not null || _targetPosition is not { } target)
			return;

		var body = Body;
		var weight = 1 - MathF.Exp(-SmoothingRate * (float)delta);

		body.GlobalPosition = body.GlobalPosition.DistanceTo(target) > SnapDistance
			? target
			: body.GlobalPosition.Lerp(target, weight);

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
			if (!GSessionManager.TryGetPeerId(Id, out var ownerId) || ownerId != senderId)
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
			if (peerId != sourcePeerId && peerId != GSessionManager.LocalPeerId)
				RpcId(peerId, MethodName.RpcReplicate, position, yaw);
	}
}
