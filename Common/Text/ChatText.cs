using System.Globalization;
using System.Text;

namespace EnsembleRoot.Common.Text;

/// <summary>Normalizes chat messages, so every peer can check that one is valid and in its normalized form.</summary>
public static class ChatText
{
	public const int MaxLength = 256;

	/// <summary>
	///     Trims <paramref name="text" />, collapses its whitespace (including line breaks) into single spaces, drops
	///     its other control and format characters, and truncates it to <see cref="MaxLength" />.
	/// </summary>
	public static string Sanitize(string text)
	{
		var sanitized = new StringBuilder(Math.Min(text.Length, MaxLength));

		foreach (var c in text.AsSpan().Trim())
		{
			if (char.IsWhiteSpace(c))
			{
				if (sanitized.Length > 0 && sanitized[^1] is not ' ')
					sanitized.Append(' ');
			}
			else if (char.GetUnicodeCategory(c) is not (UnicodeCategory.Control or UnicodeCategory.Format))
				sanitized.Append(c);

			if (sanitized.Length >= MaxLength)
				break;
		}

		// Never split a surrogate pair
		if (sanitized.Length > 0 && char.IsHighSurrogate(sanitized[^1]))
			sanitized.Length--;

		return sanitized.ToString().TrimEnd();
	}

	public static bool IsValid(string text) =>
		text.Length > 0 && string.Equals(Sanitize(text), text, StringComparison.Ordinal);
}
