using System.Globalization;
using Avalonia.Media;

namespace EnsembleRoot.Ui.ViewModels.Utils;

public enum ColorEntryMode
{
	Rgb,
	Hsv,
	Hex
}

/// <summary>
///     The paint palette's color text: hex as the game stores colors (<c>#RRGGBB</c> or <c>#RRGGBBAA</c>), or channel
///     values separated by commas or spaces: red, green, blue, and alpha from 0 to 255, or hue (0 to 360), saturation
///     and value (0 to 100), and alpha (0 to 255). Bare values read in the given mode; <c>rgb(...)</c> and
///     <c>hsv(...)</c> read the same in any.
/// </summary>
public static class PaintColorFormat
{
	private const string RgbPrefix = "rgb";
	private const string HsvPrefix = "hsv";

	private static readonly char[] Separators = [',', ' ', '\t'];

	public static string Format(HsvColor color, ColorEntryMode mode)
	{
		var rgb = color.ToRgb();

		return mode switch
		{
			ColorEntryMode.Hsv => Join(Round(color.H), Round(color.S * 100), Round(color.V * 100), rgb.A),
			ColorEntryMode.Hex => rgb.A is byte.MaxValue
				? $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}"
				: $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}{rgb.A:X2}",
			_ => Join(rgb.R, rgb.G, rgb.B, rgb.A)
		};
	}

	public static bool TryParse(string text, ColorEntryMode mode, out HsvColor color)
	{
		color = default;
		text = text.Trim();

		if (TryParseHex(text, out var hex))
		{
			color = hex.ToHsv();
			return true;
		}

		var isHsv = mode is ColorEntryMode.Hsv;

		if (TryUnwrap(ref text, RgbPrefix))
			isHsv = false;
		else if (TryUnwrap(ref text, HsvPrefix))
			isHsv = true;

		var values = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

		if (values.Length is not (3 or 4) || !TryParseValues(values, out var numbers))
			return false;

		var alpha = numbers.Length is 4 ? numbers[3] : byte.MaxValue;

		if (alpha > byte.MaxValue)
			return false;

		if (isHsv)
		{
			if (numbers[0] > 360 || numbers[1] > 100 || numbers[2] > 100)
				return false;

			color = new HsvColor(alpha / 255d, numbers[0], numbers[1] / 100d, numbers[2] / 100d);
			return true;
		}

		if (numbers[..3].Any(static number => number > byte.MaxValue))
			return false;

		color = Color.FromArgb((byte)alpha, (byte)numbers[0], (byte)numbers[1], (byte)numbers[2]).ToHsv();
		return true;
	}

	private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

	private static string Join(params ReadOnlySpan<int> values) =>
		string.Join(", ", values.ToArray().Select(static value => value.ToString(CultureInfo.InvariantCulture)));

	// Six or eight digits, with or without '#'
	private static bool TryParseHex(string text, out Color color)
	{
		color = default;
		var digits = text.StartsWith('#', StringComparison.Ordinal) ? text[1..] : text;

		if (
			digits.Length is not (6 or 8) ||
			!uint.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
			return false;

		var rgba = digits.Length is 6 ? (value << 8) | byte.MaxValue : value;
		color = Color.FromArgb((byte)rgba, (byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8));

		return true;
	}

	private static bool TryUnwrap(ref string text, string prefix)
	{
		if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			return false;

		var inner = text[prefix.Length..].TrimStart('a', 'A').Trim();

		if (!inner.StartsWith('(', StringComparison.Ordinal) || !inner.EndsWith(')', StringComparison.Ordinal))
			return false;

		text = inner[1..^1];
		return true;
	}

	private static bool TryParseValues(string[] values, out int[] numbers)
	{
		numbers = new int[values.Length];

		for (var i = 0; i < values.Length; i++)
			if (!int.TryParse(values[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]))
				return false;

		return true;
	}
}
