using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using Serilog;

namespace EnsembleRoot.Sessions;

public partial class SessionManager
{
	private const string RateLimitedReason = "Too many requests. Slow down.";

	internal void Submit<TAction>(TAction action) where TAction : INetworkAction<TAction>
	{
		if (!IsActive)
		{
			Log.Warning("Dropped action {ActionId}: no active session", TAction.Id);
			return;
		}

		if (IsServer)
			HandleAction(TAction.Id, action.ToPayload(), LocalPeerId);
		else
			RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcRequestAction, TAction.Id, action.ToPayload());
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcRequestAction(string actionId, Array<Variant> payload)
	{
		if (!IsServer)
			return;

		var senderId = Multiplayer.GetRemoteSenderId();
		var tokenCost = NetworkActionRegistry.GetTokenCost(actionId);

		EnqueueRpc(
			senderId, tokenCost, () => HandleAction(actionId, payload, senderId),
			() => RejectAction(actionId, RateLimitedReason, senderId));
	}

	[Rpc]
	private void RpcConfirmAction(string actionId, Array<Variant> payload, int sourcePeerId)
	{
		var result = ExecuteAction(actionId, ref payload, sourcePeerId, false);

		if (!result.IsValid)
			Log.Warning(
				"Discarded confirmed action {ActionId} from peer {PeerId}: {Reason}",
				actionId, sourcePeerId, result.Reason);
	}

	[Rpc]
	private void RpcRejectAction(string actionId, string reason) =>
		EmitSignal(SignalName.ActionRejected, actionId, reason);

	private void HandleAction(string actionId, Array<Variant> payload, int sourcePeerId)
	{
		var result = ExecuteAction(actionId, ref payload, sourcePeerId, true);

		if (result.IsValid)
		{
			RpcSynced(MethodName.RpcConfirmAction, actionId, payload, sourcePeerId);
			return;
		}

		var reason = result.Reason ?? string.Empty;
		Log.Debug("Rejected action {ActionId} from peer {PeerId}: {Reason}", actionId, sourcePeerId, reason);

		RejectAction(actionId, reason, sourcePeerId);
	}

	private void RejectAction(string actionId, string reason, int sourcePeerId)
	{
		// The sender may have disconnected while its request waited on the rate limiter
		if (sourcePeerId == LocalPeerId)
			EmitSignal(SignalName.ActionRejected, actionId, reason);
		else if (Multiplayer.GetPeers().Contains(sourcePeerId))
			RpcId(sourcePeerId, MethodName.RpcRejectAction, actionId, reason);
	}

	private ActionValidation ExecuteAction(
		string actionId, ref Array<Variant> payload, int sourcePeerId, bool isHost)
	{
		if (!_peersById.TryGetValue(sourcePeerId, out var peer))
			return ActionValidation.Reject("Peer not registered.");

		var source = new ActionSource(sourcePeerId, peer.PlayerId);
		return NetworkActionRegistry.Execute(actionId, ref payload, source, isHost);
	}
}
