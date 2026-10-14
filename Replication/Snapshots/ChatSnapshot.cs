using System.Runtime.CompilerServices;
using System.Text;
using EnsembleRoot.Scripts.Chat;
using EnsembleRoot.Sessions.Snapshots;
using Godot;
using Godot.Collections;
using Serilog;

namespace EnsembleRoot.Replication.Snapshots;

internal static class ChatSnapshot
{
	// Bounds the payload, whatever the messages' lengths and encodings
	private const int MaxTextBytes = 32 * 1024;

	private static Array<Variant> Capture()
	{
		var messages = new Array<Dictionary>();
		var textBytes = 0;

		// Keeps the newest messages that fit
		foreach (var message in GChatManager.Messages.Reverse())
		{
			textBytes += Encoding.UTF8.GetByteCount(message.SenderName) + Encoding.UTF8.GetByteCount(message.Text);

			if (textBytes > MaxTextBytes)
				break;

			messages.Insert(0, message.ToDict());
		}

		return [messages];
	}

	private static void Restore(Array<Variant> payload)
	{
		foreach (var dict in payload[0].AsGodotArray<Dictionary>())
			if (ChatMessage.TryFromDict(dict) is { } message)
				GChatManager.Add(message, true);
			else
				Log.Warning("Skipped malformed chat message in snapshot");
	}

	[ModuleInitializer]
	internal static void Register() => NetworkSnapshotRegistry.Register(nameof(ChatSnapshot), Capture, Restore);
}
