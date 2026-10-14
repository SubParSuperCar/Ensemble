using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Serilog;

namespace EnsembleRoot.Common.Text;

/// <summary>
///     Masks profanity in text (e.g., chat messages) with <see cref="Mask" /> characters, or replaces the text with
///     <see cref="Redacted" /> if nothing else would remain (or <see cref="Failed" /> if matching times out or the list
///     failed to load).
///     Thread-safe.
/// </summary>
/// <remarks>
///     Uses David Sojevic's English profanity-list (MIT; see ProfanityList.LICENSE.txt), matching case-insensitively
///     and through common substitutions (e.g., "4" for "a"). Entries also match inside words, masking the whole word,
///     unless they set <c>allow_partial</c> to false. The bundled copy sets it on the entries whose partial matches hit
///     ordinary words (e.g., "class" or "grape"); those only match as words, optionally inflected. Each entry's
///     exceptions (e.g., "sp*" for "sparse") are honored.
///     <para>
///         The list is embedded GZip-compressed, so its terms can't be read or searched in the source. Convert it
///         with .github/scripts/profanity-list.sh.
///     </para>
/// </remarks>
public static class ProfanityFilter
{
	// ReSharper disable once MemberCanBePrivate.Global
	public const char Mask = '#';

	public const string Redacted = "<Redacted>";
	public const string Failed = "<Filter Error>";

	private const string ListResourceName = "EnsembleRoot.Common.Text.ProfanityList.json.gz";
	private const string CoreGroup = "core";

	private const string WordStart = @"(?<![\p{L}\p{N}])";
	private const string WordEnd = @"(?![\p{L}\p{N}])";
	private const string Separators = @"[\W_]*";

	// Optionally doubles the last letter, as in "stopped"
	private const string Inflection = @"(?:(?<=(?<last>.))\k<last>?(?:s|es|ed|er|ers|ing|y))?";

	private const RegexOptions PatternOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

	private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(RegexMatchTimeoutMs);

	private static readonly Dictionary<char, string> Substitutes = new()
	{
		['a'] = "4@",
		['b'] = "8",
		['e'] = "3",
		['g'] = "9",
		['i'] = "1!",
		['l'] = "1",
		['o'] = "0",
		['s'] = "5$",
		['t'] = "7"
	};

	private static readonly Lazy<Entry[]> Entries = new(Load);

	private static int _hasLoggedLoadFailure;

	/// <summary>Loads the list on the thread pool, so the first <see cref="Censor" /> call doesn't stall.</summary>
	public static void Preload() =>
		_ = Task.Run(static () =>
		{
			try
			{
				_ = Entries.Value;
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Failed to load the profanity list");
			}
		});

	/// <summary>
	///     Masks the profanity in <paramref name="text" />, or returns <see cref="Redacted" /> if no letter or digit
	///     would remain unmasked, or <see cref="Failed" /> if matching times out or the list failed to load.
	/// </summary>
	public static string Censor(string text)
	{
		Entry[] entries;

		try
		{
			entries = Entries.Value;
		}
		// The lazy list caches its load failure, so it would otherwise rethrow on every call
		catch (Exception exception)
		{
			if (Interlocked.Exchange(ref _hasLoggedLoadFailure, 1) is 0)
				Log.Error(exception, "Failed to load the profanity list");

			return Failed;
		}

		char[]? censored = null;

		try
		{
			foreach (var entry in entries)
				for (var match = entry.Pattern.Match(text); match.Success; match = match.NextMatch())
				{
					var core = match.Groups[CoreGroup];

					if (entry.IsException(text, core.Index, core.Length))
						continue;

					var (start, end) = (match.Index, match.Index + match.Length);

					if (entry.AllowsPartial)
						(start, end) = ExpandToWord(text, start, end);

					censored ??= text.ToCharArray();
					censored.AsSpan(start, end - start).Fill(Mask);
				}
		}
		catch (RegexMatchTimeoutException exception)
		{
			Log.Warning(exception, "Profanity filter timed out (Length={Length})", text.Length);
			return Failed;
		}


		if (censored is null)
			return text;

		return Array.Exists(censored, char.IsLetterOrDigit) ? new string(censored) : Redacted;
	}

	private static (int Start, int End) ExpandToWord(string text, int start, int end)
	{
		while (start > 0 && char.IsLetterOrDigit(text[start - 1]))
			start--;

		while (end < text.Length && char.IsLetterOrDigit(text[end]))
			end++;

		return (start, end);
	}

	private static Entry[] Load()
	{
		using var compressed =
			typeof(ProfanityFilter).Assembly.GetManifestResourceStream(ListResourceName) ??
			throw new InvalidOperationException($"Embedded resource {ListResourceName} not found.");

		using var json = new GZipStream(compressed, CompressionMode.Decompress);
		using var document = JsonDocument.Parse(json);

		return [.. document.RootElement.EnumerateArray().Select(CreateEntry)];
	}

	private static Entry CreateEntry(JsonElement element)
	{
		var allowsPartial = !element.TryGetProperty("allow_partial", out var allowPartial) || allowPartial.GetBoolean();
		var alternatives = string.Join('|', element.GetProperty("match").GetString()!.Split('|').Select(ToPattern));
		var core = $"(?<{CoreGroup}>{alternatives})";

		var exceptions = element.TryGetProperty("exceptions", out var exceptionArray)
			? exceptionArray.EnumerateArray().Select(static exception => exception.GetString()!.Split('*', 2))
				.Select(static parts => (parts[0], parts[1]))
				.ToArray()
			: [];

		return new Entry(
			new Regex(allowsPartial ? core : WordStart + core + Inflection + WordEnd, PatternOptions, MatchTimeout),
			allowsPartial, exceptions);
	}

	// In the list's syntax, "*" repeats the previous character, and spaces, hyphens, and dots separate words
	private static string ToPattern(string match)
	{
		var pattern = new StringBuilder();

		foreach (var c in match)
			switch (c)
			{
				case '*':
					pattern.Append('+');
					break;

				case ' ' or '-' or '.':
					pattern.Append(Separators);
					break;

				default:
					var characters = c + Substitutes.GetValueOrDefault(c, string.Empty);
					pattern.Append('[').Append(Regex.Escape(characters)).Append(']');
					break;
			}

		return pattern.ToString();
	}

	private sealed record Entry(Regex Pattern, bool AllowsPartial, (string Prefix, string Suffix)[] Exceptions)
	{
		public bool IsException(string text, int index, int length) =>
			Exceptions.Any(exception =>
				index >= exception.Prefix.Length &&
				text.AsSpan(index - exception.Prefix.Length, exception.Prefix.Length)
					.Equals(exception.Prefix, StringComparison.OrdinalIgnoreCase) &&
				text.AsSpan(index + length).StartsWith(exception.Suffix, StringComparison.OrdinalIgnoreCase));
	}
}
