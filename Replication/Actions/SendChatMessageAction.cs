using System.Runtime.CompilerServices;
using EnsembleRoot.Common.Text;
using EnsembleRoot.Scripts.Chat;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

public readonly record struct SendChatMessageAction(string Text) : INetworkAction<SendChatMessageAction>
{
	public static int TokenCost { get; } = 10;

	public static SendChatMessageAction FromPayload(Array<Variant> payload) => new(payload[0].AsString());

	public Array<Variant> ToPayload() => [Text];

	public ActionValidation Validate(ActionSource source) =>
		ChatText.IsValid(Text) ? Accept : Reject("Invalid chat message.");

	public SendChatMessageAction Rewrite() =>
		GChatManager.IsFilterEnabled ? new SendChatMessageAction(ProfanityFilter.Censor(Text)) : this;

	public void Apply(ActionSource source) =>
		GChatManager.Add(
			new ChatMessage
			{
				SenderId = source.PlayerId,
				SenderName = GPlayers.GetPlayer(source.PlayerId)?.Name ?? string.Empty,
				Text = Text
			});

	/// <summary>Sanitizes and sends <paramref name="text" />, unless nothing is left of it.</summary>
	public static void Send(string text)
	{
		if (ChatText.Sanitize(text) is { Length: > 0 } sanitized)
			new SendChatMessageAction(sanitized).Submit();
	}

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<SendChatMessageAction>();
}
