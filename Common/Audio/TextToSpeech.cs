using System.Threading.Channels;
using BogaNet.TTS;
using Serilog;

namespace EnsembleRoot.Common.Audio;

/// <summary>
///     Reads text aloud one utterance at a time, in order, dropping new text while <see cref="MaxQueuedCount" /> are
///     waiting. Never throws: speech depends on each platform's voices and helper binaries, which have gone missing.
/// </summary>
/// <remarks>
///     Canceling an utterance's token skips it while it waits, or stops it mid-speech, so a caller (e.g., the chat)
///     can silence its own text without cutting off anyone else's.
/// </remarks>
public static class TextToSpeech
{
	private const int MaxQueuedCount = 8;

	private static readonly Channel<Utterance> Queue = Channel.CreateBounded<Utterance>(
		new BoundedChannelOptions(MaxQueuedCount) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

	static TextToSpeech()
	{
		_ = Task.Run(SpeakQueuedAsync);
	}

	public static void Speak(
		string text, string culture = "en", float rate = 1f, float pitch = 1f, float volume = 1f,
		CancellationToken token = default)
	{
		if (!Queue.Writer.TryWrite(new Utterance(text, culture, rate, pitch, volume, token)))
			Log.Debug("Skipped TTS, since too much text is waiting");
	}

	private static async Task SpeakQueuedAsync()
	{
		await foreach (
			var (text, culture, rate, pitch, volume, token) in Queue.Reader.ReadAllAsync().ConfigureAwait(false))
		{
			if (token.IsCancellationRequested)
				continue;

			try
			{
				var speaker = Speaker.Instance;
				var voice = speaker.VoiceForCulture(culture);

				// Only this utterance is speaking, so silencing the speaker stops nothing else
				var registration = token.Register(Silence);

				await using (registration.ConfigureAwait(false))
				{
					// Canceled before registering, Silence already ran and would stop nothing
					if (token.IsCancellationRequested)
						continue;

					if (
						!await speaker.SpeakAsync(text, voice, rate, pitch, volume).ConfigureAwait(false) &&
						!token.IsCancellationRequested)
						Log.Warning("Failed to play TTS");
				}
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Failed to play TTS");
			}
		}
	}

	private static void Silence()
	{
		try
		{
			Speaker.Instance.Silence();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to silence TTS");
		}
	}

	private readonly record struct Utterance(
		string Text,
		string Culture,
		float Rate,
		float Pitch,
		float Volume,
		CancellationToken Token);
}
