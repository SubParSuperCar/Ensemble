using EnsembleRoot.SessionManager.Actions;
using Godot;
using Godot.Collections;
using Serilog;

namespace EnsembleRoot.SessionManager;

public partial class SessionManager
{
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
			RpcId(ServerPeerId, MethodName.RpcRequestAction, TAction.Id, action.ToPayload());
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcRequestAction(string actionId, Array<Variant> payload)
	{
		var senderId = Multiplayer.GetRemoteSenderId();

		EnqueueRpc(
			senderId,
			NetworkActionRegistry.GetTokenCost(actionId),
			() => HandleAction(actionId, payload, senderId));
	}

	[Rpc]
	private void RpcConfirmAction(string actionId, Array<Variant> payload, int sourcePeerId)
	{
		var result = ExecuteAction(actionId, payload, sourcePeerId);

		if (!result.IsValid)
			Log.Warning(
				"Discarded confirmed action {ActionId} from peer {PeerId}: {Reason}",
				actionId,
				sourcePeerId,
				result.Reason);
	}

	[Rpc]
	private void RpcRejectAction(string actionId, string reason) =>
		EmitSignal(SignalName.ActionRejected, actionId, reason);

	private void HandleAction(string actionId, Array<Variant> payload, int sourcePeerId)
	{
		var result = ExecuteAction(actionId, payload, sourcePeerId);

		if (result.IsValid)
		{
			RpcSynced(MethodName.RpcConfirmAction, actionId, payload, sourcePeerId);
			return;
		}

		var reason = result.Reason ?? string.Empty;
		Log.Debug("Rejected action {ActionId} from peer {PeerId}: {Reason}", actionId, sourcePeerId, reason);

		if (sourcePeerId == LocalPeerId)
			EmitSignal(SignalName.ActionRejected, actionId, reason);
		else
			RpcId(sourcePeerId, MethodName.RpcRejectAction, actionId, reason);
	}

	private ActionValidation ExecuteAction(string actionId, Array<Variant> payload, int sourcePeerId) =>
		_peersById.TryGetValue(sourcePeerId, out var info)
			? NetworkActionRegistry.Execute(actionId, payload, new ActionSource(sourcePeerId, info.PlayerId))
			: ActionValidation.Reject("Peer not registered.");
}
