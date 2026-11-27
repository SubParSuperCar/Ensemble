using System.Globalization;
using EnsembleRoot.Ui.Controls;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class NumberExpressionTests
{
	[Theory]
	[InlineData("2.5", "en-US", "2.5")]
	[InlineData(" 1 + 2 * 3 ", "en-US", "7")]
	[InlineData("(1 + 2) * 3", "en-US", "9")]
	[InlineData("-2 - -3", "en-US", "1")]
	[InlineData("10 / 4", "en-US", "2.5")]
	[InlineData("1,000 / 8", "en-US", "125")]
	[InlineData("2e3 - 1.5E-1", "en-US", "1999.85")]
	[InlineData("1,5 * 2", "de-DE", "3")]
	public void Evaluate_Valid_Computes(string text, string culture, string expected) =>
		Assert.Equal(
			decimal.Parse(expected, CultureInfo.InvariantCulture),
			NumberExpression.Evaluate(text, CultureInfo.GetCultureInfo(culture).NumberFormat));

	[Fact]
	public void Evaluate_Thirds_KeepsDecimalPrecision() =>
		Assert.Equal(1m / 3, NumberExpression.Evaluate("1/3", NumberFormatInfo.InvariantInfo));

	[Theory]
	[InlineData("")]
	[InlineData("1 +")]
	[InlineData("(1")]
	[InlineData("1)")]
	[InlineData("2 3")]
	[InlineData("1 / 0")]
	[InlineData("x")]
	[InlineData("1e")]
	public void TryEvaluate_Invalid_Fails(string text) =>
		Assert.False(NumberExpression.TryEvaluate(text, NumberFormatInfo.InvariantInfo, out _));
}
