using Avalonia.Media;
using EnsembleRoot.Ui.ViewModels.Utils;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class PaintColorFormatTests
{
	[Theory]
	[InlineData("#FF8000", ColorEntryMode.Rgb, 255, 128, 0, 255)]
	[InlineData("ff800080", ColorEntryMode.Hsv, 255, 128, 0, 128)]
	[InlineData(" 255, 128, 0 ", ColorEntryMode.Rgb, 255, 128, 0, 255)]
	[InlineData("255 128 0 64", ColorEntryMode.Hex, 255, 128, 0, 64)]
	[InlineData("0, 100, 100", ColorEntryMode.Hsv, 255, 0, 0, 255)]
	[InlineData("RGB(0, 0, 255)", ColorEntryMode.Hsv, 0, 0, 255, 255)]
	[InlineData("hsva(120, 100, 100, 0)", ColorEntryMode.Rgb, 0, 255, 0, 0)]
	public void TryParse_Valid_Succeeds(string text, ColorEntryMode mode, int r, int g, int b, int a)
	{
		Assert.True(PaintColorFormat.TryParse(text, mode, out var color));
		Assert.Equal(Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b), color.ToRgb());
	}

	[Theory]
	[InlineData("")]
	[InlineData("#FF80")]
	[InlineData("#GG8000")]
	[InlineData("256, 0, 0")]
	[InlineData("1, 2")]
	[InlineData("1, 2, 3, 4, 5")]
	[InlineData("-1, 0, 0")]
	[InlineData("1.5, 0, 0")]
	[InlineData("hsv(361, 0, 0)")]
	[InlineData("hsv(0, 101, 0)")]
	[InlineData("rgb(1, 2, 3")]
	public void TryParse_Invalid_Fails(string text) =>
		Assert.False(PaintColorFormat.TryParse(text, ColorEntryMode.Rgb, out _));

	[Theory]
	[InlineData(ColorEntryMode.Rgb, "255, 128, 0, 255")]
	[InlineData(ColorEntryMode.Hex, "#FF8000")]
	[InlineData(ColorEntryMode.Hsv, "30, 100, 100, 255")]
	public void Format_RoundTrips(ColorEntryMode mode, string expected)
	{
		var color = Color.FromRgb(255, 128, 0).ToHsv();
		var text = PaintColorFormat.Format(color, mode);

		Assert.Equal(expected, text);
		Assert.True(PaintColorFormat.TryParse(text, mode, out var parsed));
		Assert.Equal(color.ToRgb(), parsed.ToRgb());
	}

	[Fact]
	public void Format_HexKeepsAlphaOnlyWhenSeeThrough() =>
		Assert.Equal("#FF800080",
			PaintColorFormat.Format(Color.FromArgb(128, 255, 128, 0).ToHsv(), ColorEntryMode.Hex));
}
