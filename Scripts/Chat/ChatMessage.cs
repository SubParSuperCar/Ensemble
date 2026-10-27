using EnsembleRoot.Common.Text;
using Godot;
using Godot.Collections;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Chat;

/// <summary>A player's chat message, or a notice from the game if <see cref="SenderId" /> is empty.</summary>
public partial class ChatMessage : RefCounted
{
	// DateTimeOffset.MaxValue
	private const double MaxUnixSeconds = 253402300799d;

	public string SenderId { get; init; } = string.Empty;
	public string SenderName { get; init; } = string.Empty;
	public string Text { get; init; } = string.Empty;

	public DateTimeOffset UtcPostedAt { get; init; } = GTimeProvider.GetUtcNow();
	public double UtcPostedAtUnix => UtcPostedAt.ToUnixTimeMilliseconds() / 1000d;

	public bool IsNotice => SenderId.Length is 0;
	public Color SenderColor => NametagColors.For(SenderName);

	/// <summary>
	///     Returns null if <paramref name="dict" /> is malformed, e.g., in history from a misbehaving host.
	/// </summary>
	public static ChatMessage? TryFromDict(Dictionary dict)
	{
		if (
			!TryGetString(dict, "senderId", out var senderId) ||
			!TryGetString(dict, "senderName", out var senderName) || !TryGetString(dict, "text", out var text) ||
			!ChatText.IsValid(text) || (senderName.Length > 0 && !ChatText.IsValid(senderName)) ||
			!dict.TryGetValue("utcPostedAtUnix", out var postedAt) || postedAt.VariantType is not Variant.Type.Float ||
			postedAt.AsDouble() is not (>= 0d and <= MaxUnixSeconds))
			return null;

		return new ChatMessage
		{
			SenderId = senderId,
			SenderName = senderName,
			Text = text,
			UtcPostedAt = DateTimeOffset.UnixEpoch + TimeSpan.FromSeconds(postedAt.AsDouble())
		};

		static bool TryGetString(Dictionary dict, string key, out string value)
		{
			var isString = dict.TryGetValue(key, out var variant) && variant.VariantType is Variant.Type.String;
			value = isString ? variant.AsString() : string.Empty;

			return isString;
		}
	}

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
