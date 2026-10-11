using System.Threading.Channels;
using BogaNet.TTS;
using Serilog;

namespace EnsembleRoot.Common.Audio;

/// <summary>
///     Reads text aloud one utterance at a time, in order, dropping new text while <see cref="MaxQueuedCount" /> are
///     waiting. Never throws: speech depends on each platform's voices and helper binaries, which have gone missing.
/// </summary>
public static class TextToSpeech
{
	// ReSharper disable once MemberCanBePrivate.Global
	public const int MaxQueuedCount = 8;

	private static readonly Channel<Utterance> Queue = Channel.CreateBounded<Utterance>(
		new BoundedChannelOptions(MaxQueuedCount) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

	static TextToSpeech()
	{
		_ = Task.Run(SpeakQueuedAsync);
	}

	public static void Speak(string text, string culture = "en", float rate = 1f, float pitch = 1f, float volume = 1f)
	{
		if (!Queue.Writer.TryWrite(new Utterance(text, culture, rate, pitch, volume)))
			Log.Debug("Skipped TTS, since too much text is waiting");
	}

	/// <summary>Stops the current utterance and drops the waiting ones.</summary>
	public static void Silence()
	{
		while (Queue.Reader.TryRead(out _)) { }

		try
		{
			Speaker.Instance.Silence();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to silence TTS");
		}
	}

	private static async Task SpeakQueuedAsync()
	{
		await foreach (var (text, culture, rate, pitch, volume) in Queue.Reader.ReadAllAsync().ConfigureAwait(false))
		{
			try
			{
				var speaker = Speaker.Instance;
				var voice = speaker.VoiceForCulture(culture);

				if (!await speaker.SpeakAsync(text, voice, rate, pitch, volume).ConfigureAwait(false))
					Log.Warning("Failed to play TTS");
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Failed to play TTS");
			}
		}
	}

	private readonly record struct Utterance(string Text, string Culture, float Rate, float Pitch, float Volume);
}
