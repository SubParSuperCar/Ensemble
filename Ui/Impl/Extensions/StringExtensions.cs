using System.Text.RegularExpressions;

namespace EnsembleRoot.Ui.Extensions;

public static partial class StringExtensions
{
	// Between a lowercase letter or digit and an uppercase one, between an acronym and the next word, or a separator
	[GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|[\\s_-]+", RegexOptions.None, RegexMatchTimeoutMs)]
	private static partial Regex WordBoundaryRegex { get; }

	/// <summary>Turns an identifier into capitalized words, e.g., <c>targetLength</c> into "Target Length".</summary>
	public static string ToDisplayName(this string identifier) =>
		string.Join(' ',
			WordBoundaryRegex.Split(identifier)
				.Where(static word => word.Length > 0)
				.Select(static word => string.Concat(char.ToUpperInvariant(word[0]).ToString(), word[1..])));
}
