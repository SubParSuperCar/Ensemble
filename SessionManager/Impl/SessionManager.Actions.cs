using Godot;
using Godot.Collections;
using Root.SessionManager.Actions;

namespace Root.SessionManager;

// Would this support a case where instead of Host being authority for an op, it were say, a Plot owner?
// For instance, if we wanted to optimize by letting plot owners have network authority over their plots,
// we'd treat them as the sanity checker for that area. Like, if someone wants to place a block, it goes through the
// owner instead of the session host. Also, we may also want to perform sanity checks as listeners too, even if we're a
// client. For example, if the host says to do something that's impossible, or they'd deny themselves, we deny it too.
// If they say to add more than the maximum number allowed blocks, deny it, even as a client. That sort of thing.
// This may be a worthwhile improvement but not urgently necessary. It's probably fine if Host is the pure authority,
// but if possible, we could treat plot owners as authorities while still making calls as authority distinct. Such as
// not sending a request and rather just telling other clients to sync up when the Plot owner decides to place a block.
// Even though the API call to place a block looks the same with or without being owner/authority.
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
