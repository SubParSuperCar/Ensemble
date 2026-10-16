using System.Globalization;

namespace EnsembleRoot.Common.Text;

public static class TextElements
{
	/// <summary>
	///     Truncates <paramref name="value" /> to at most <paramref name="maxLength" /> text elements, the last of
	///     which is an ellipsis if any were cut.
	/// </summary>
	public static string Truncate(string value, int maxLength)
	{
		if (maxLength <= 0)
			return string.Empty;

		var elements = StringInfo.ParseCombiningCharacters(value);

		if (elements.Length <= maxLength)
			return value;

		var end = elements[maxLength - 1];
		return value[..end] + '\u2026';
	}
}
