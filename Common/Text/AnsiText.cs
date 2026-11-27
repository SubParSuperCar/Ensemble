using System.Globalization;
using System.Text;

namespace EnsembleRoot.Common.Text;

/// <summary>A foreground as an xterm 256-color index (none for the default), and whether text is bold.</summary>
public readonly record struct AnsiStyle(int? Foreground, bool IsBold);

public readonly record struct AnsiRun(int Start, int Length, AnsiStyle Style);

/// <summary>Reads the styles ANSI escape codes (SGR sequences) give text, as a terminal shows them.</summary>
public static class AnsiText
{
	private const char Escape = '\e';

	/// <summary>
	///     Splits <paramref name="text" /> into what it reads as, without its escape sequences, and the runs they
	///     style. Backgrounds, 24-bit colors, and other attributes are dropped.
	/// </summary>
	public static (string Text, AnsiRun[] Runs) Parse(string text)
	{
		if (!text.Contains(Escape, StringComparison.Ordinal))
			return (text, []);

		var plain = new StringBuilder(text.Length);
		var runs = new List<AnsiRun>();
		var style = default(AnsiStyle);
		var runStart = 0;

		for (var i = 0; i < text.Length; i++)
		{
			if (text[i] is not Escape)
			{
				plain.Append(text[i]);
				continue;
			}

			if (i + 1 >= text.Length || text[i + 1] is not '[')
				continue;

			// A control sequence is its parameters, then a final byte, which is SGR's for styles
			var end = i + 2;

			while (end < text.Length && text[end] is not (>= '@' and <= '~'))
				end++;

			if (
				end < text.Length && text[end] is 'm' && Apply(style, text.AsSpan(i + 2, end - i - 2)) is var next &&
				next != style)
			{
				AddRun(runs, runStart, plain.Length, style);
				runStart = plain.Length;
				style = next;
			}

			i = end;
		}

		AddRun(runs, runStart, plain.Length, style);
		return (plain.ToString(), [.. runs]);
	}

	private static void AddRun(List<AnsiRun> runs, int start, int end, AnsiStyle style)
	{
		if (end > start && style != default)
			runs.Add(new AnsiRun(start, end - start, style));
	}

	// An empty parameter means 0, so a bare sequence resets
	private static AnsiStyle Apply(AnsiStyle style, ReadOnlySpan<char> parameters)
	{
		var codes = new List<int>();

		foreach (var range in parameters.Split(';'))
			codes.Add(int.TryParse(parameters[range], CultureInfo.InvariantCulture, out var code) ? code : 0);

		for (var i = 0; i < codes.Count; i++)
			switch (codes[i])
			{
				case 0:
					style = default;
					break;

				case 1:
					style = style with { IsBold = true };
					break;

				case 22:
					style = style with { IsBold = false };
					break;

				case >= 30 and <= 37:
					style = style with { Foreground = codes[i] - 30 };
					break;

				case 39:
					style = style with { Foreground = null };
					break;

				case >= 90 and <= 97:
					style = style with { Foreground = codes[i] - 90 + 8 };
					break;

				// An extended foreground or background: 5 and an index, or 2 and red, green, and blue
				case 38 or 48 when i + 1 < codes.Count:
					if (codes[i] is 38 && codes[i + 1] is 5 && i + 2 < codes.Count)
						style = style with { Foreground = Math.Clamp(codes[i + 2], 0, 255) };

					i += codes[i + 1] is 5 ? 2 : 4;
					break;
			}

		return style;
	}
}
