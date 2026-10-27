using EnsembleRoot.Common.Text;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class ChatTextTests
{
	[Theory]
	[InlineData("  hi  ", "hi")]
	[InlineData("a \t\r\n b", "a b")]
	[InlineData("a\ab\u200Bc", "abc")]
	[InlineData("a \a b", "a b")]
	[InlineData("trailing control \a", "trailing control")]
	[InlineData("\a", "")]
	public void Sanitize_Normalizes(string text, string expected)
	{
		var sanitized = ChatText.Sanitize(text);

		Assert.Equal(expected, sanitized);
		Assert.Equal(sanitized, ChatText.Sanitize(sanitized));
	}

	[Fact]
	public void Sanitize_LongText_TruncatesWithoutSplittingSurrogates()
	{
		var text = new string('a', ChatText.MaxLength - 1) + "\U0001F600";
		var sanitized = ChatText.Sanitize(text);

		Assert.Equal(ChatText.MaxLength - 1, sanitized.Length);
		Assert.True(ChatText.IsValid(sanitized));
	}

	[Theory]
	[InlineData("")]
	[InlineData(" hi")]
	[InlineData("a  b")]
	[InlineData("bell\a")]
	public void IsValid_UnsanitizedText_IsFalse(string text) => Assert.False(ChatText.IsValid(text));

	[Theory]
	[InlineData("Player", "Player")]
	[InlineData("Sixteen Letters!", "Sixteen Letters!")]
	[InlineData("Player eXJNrfvKSk-OoedEyZhaZg", "Player eXJNrfvK\u2026")]
	[InlineData("Fourteen chars\U0001F600!", "Fourteen chars\u2026")]
	public void ShortenName_CapsLengthWithoutSplittingSurrogates(string name, string expected) =>
		Assert.Equal(expected, ChatText.ShortenName(name));
}
