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
	[InlineData("\a", "")]
	public void Sanitize_Normalizes(string text, string expected) => Assert.Equal(expected, ChatText.Sanitize(text));

	[Fact]
	public void Sanitize_LongText_TruncatesWithoutSplittingSurrogates()
	{
		var text = new string('a', ChatText.MaxLength - 1) + "\U0001F600";
		var sanitized = ChatText.Sanitize(text);

		Assert.Equal(ChatText.MaxLength - 1, sanitized.Length);
		Assert.True(ChatText.IsValid(sanitized));
	}

	[Theory]
	[InlineData("hi")]
	[InlineData(" padded ")]
	[InlineData("tab\tbed")]
	[InlineData("trailing control \a")]
	public void Sanitize_Output_IsValid(string text) => Assert.True(ChatText.IsValid(ChatText.Sanitize(text)));

	[Theory]
	[InlineData("")]
	[InlineData(" hi")]
	[InlineData("a  b")]
	[InlineData("bell\a")]
	public void IsValid_UnsanitizedText_IsFalse(string text) => Assert.False(ChatText.IsValid(text));
}
