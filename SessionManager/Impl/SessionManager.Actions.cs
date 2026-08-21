using Godot;
using Godot.Collections;
using Root.SessionManager.Actions;
using Serilog;

namespace Root.SessionManager;

public partial class SessionManager
{
	// ReSharper disable once MemberCanBePrivate.Global
	public NetworkActionRegistry Actions { get; } = new();

	// ReSharper disable once EventNeverSubscribedTo.Global
	public event Action<string, string>? ActionRejected;

	// ReSharper disable once UnusedMember.Global
	public void Submit<TAction>(TAction action) where TAction : INetworkAction<TAction>
	{
		var payload = action.ToPayload();

		if (IsServer)
			TryApplyAndBroadcast(TAction.ActionId, payload, LocalPeerId, false);
		else
			RpcId(1, MethodName.RpcRequestAction, TAction.ActionId, payload);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcRequestAction(string actionId, Dictionary payload)
	{
		var senderId = Multiplayer.GetRemoteSenderId();
		EnqueueRpc(senderId, 1,
			() => TryApplyAndBroadcast(actionId, payload, senderId, true));
	}

	private void TryApplyAndBroadcast(string actionId, Dictionary payload, int sourcePeerId, bool notifyRejection)
	{
		var result = Actions.ValidateRaw(actionId, payload, sourcePeerId);

		if (!result.IsValid)
		{
			Log.Debug("Rejected action {ActionId} from peer {PeerId}: {Reason}",
				actionId, sourcePeerId, result.Reason);

			if (notifyRejection)
				RpcId(sourcePeerId, MethodName.RpcRejectAction, actionId, result.Reason ?? string.Empty);

			return;
		}

		Actions.ApplyRaw(actionId, payload, sourcePeerId);
		Rpc(MethodName.RpcConfirmAction, actionId, payload, sourcePeerId);
	}

	[Rpc(CallLocal = false)]
	private void RpcConfirmAction(string actionId, Dictionary payload, int sourcePeerId) =>
		Actions.ApplyRaw(actionId, payload, sourcePeerId);

	[Rpc(CallLocal = false)]
	private void RpcRejectAction(string actionId, string reason) => ActionRejected?.Invoke(actionId, reason);
}
