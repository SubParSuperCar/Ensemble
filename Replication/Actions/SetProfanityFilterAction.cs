using System.Runtime.CompilerServices;
using EnsembleRoot.Scripts.Chat;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

public readonly record struct SetProfanityFilterAction(bool IsEnabled) : INetworkAction<SetProfanityFilterAction>
{
	public static int TokenCost { get; } = 10;

	public static SetProfanityFilterAction FromPayload(Array<Variant> payload) => new(payload[0].AsBool());

	public Array<Variant> ToPayload() => [IsEnabled];

	public ActionValidation Validate(ActionSource source) =>
		source.IsHost ? Accept : Reject("Only the host can toggle the profanity filter.");

	public void Apply(ActionSource source)
	{
		if (!GProfanityFilterManager.SetEnabled(IsEnabled))
			return;

		var state = IsEnabled ? "on" : "off";
		GChatManager.Add(new ChatMessage { Text = $"The host turned the profanity filter {state}." });
	}

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<SetProfanityFilterAction>();
}
