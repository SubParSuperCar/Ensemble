using Godot;
using Godot.Collections;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Chat;

/// <summary>A player's chat message, or a notice from the game if <see cref="SenderId" /> is empty.</summary>
public partial class ChatMessage : RefCounted
{
	public string SenderId { get; init; } = string.Empty;
	public string SenderName { get; init; } = string.Empty;
	public string Text { get; init; } = string.Empty;

	public DateTimeOffset UtcPostedAt { get; init; } = GTimeProvider.GetUtcNow();
	public double UtcPostedAtUnix => UtcPostedAt.ToUnixTimeMilliseconds() / 1000d;

	public bool IsNotice => SenderId.Length is 0;
	public Color SenderColor => NametagColors.For(SenderName);

	public static ChatMessage FromDict(Dictionary dict) =>
		new()
		{
			SenderId = dict["senderId"].AsString(),
			SenderName = dict["senderName"].AsString(),
			Text = dict["text"].AsString(),
			UtcPostedAt = DateTimeOffset.UnixEpoch + TimeSpan.FromSeconds(dict["utcPostedAtUnix"].AsDouble())
		};

	public Dictionary ToDict() =>
		new()
		{
			["senderId"] = SenderId,
			["senderName"] = SenderName,
			["text"] = Text,
			["utcPostedAtUnix"] = UtcPostedAtUnix
		};

	public override string ToString() => IsNotice ? $"* {Text}" : $"{SenderName}: {Text}";
}
