using System.Runtime.CompilerServices;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

public readonly record struct SetChatFilterAction(bool IsEnabled) : INetworkAction<SetChatFilterAction>
{
	public static int TokenCost { get; } = 10;

	public static SetChatFilterAction FromPayload(Array<Variant> payload) => new(payload[0].AsBool());

	public Array<Variant> ToPayload() => [IsEnabled];

	public ActionValidation Validate(ActionSource source) =>
		source.PeerId == MultiplayerPeer.TargetPeerServer
			? Accept
			: Reject("Only the host can toggle the chat filter.");

	public void Apply(ActionSource source) => GChatManager.SetFilterEnabled(IsEnabled, true);

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<SetChatFilterAction>();
}
