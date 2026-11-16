using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace EnsembleRoot.Ui.Controls;

/// <summary>Brushes that show a color over a checkerboard where it's see-through, as one tiled bitmap.</summary>
public static class Checkerboard
{
	private const int Size = 8;

	private static readonly Color Light = Colors.White;
	private static readonly Color Dark = Color.FromRgb(192, 192, 192);
	private static readonly Vector Dpi = new(96, 96);

	/// <summary>A solid brush for an opaque color; otherwise, the color blended over a checkerboard's tile.</summary>
	public static IBrush Under(Color color) =>
		color.A is byte.MaxValue ? new ImmutableSolidColorBrush(color) : CreateTileBrush(color);

	private static ImageBrush CreateTileBrush(Color color)
	{
		var bitmap = new WriteableBitmap(new PixelSize(Size, Size), Dpi, PixelFormat.Rgba8888, AlphaFormat.Opaque);
		var pixels = new int[Size * Size];
		var light = ToRgba(Blend(color, Light));
		var dark = ToRgba(Blend(color, Dark));

		for (var i = 0; i < pixels.Length; i++)
			pixels[i] = i % Size < Size / 2 == i / Size < Size / 2 ? light : dark;

		using (var buffer = bitmap.Lock())
			Marshal.Copy(pixels, 0, buffer.Address, pixels.Length);

		// Drawn at its own size, so no filtering blurs its squares
		return new ImageBrush(bitmap)
		{
			TileMode = TileMode.Tile,
			DestinationRect = new RelativeRect(0, 0, Size, Size, RelativeUnit.Absolute)
		};
	}

	private static Color Blend(Color top, Color bottom)
	{
		var alpha = top.A / 255d;

		return Color.FromRgb(Mix(top.R, bottom.R), Mix(top.G, bottom.G), Mix(top.B, bottom.B));

		byte Mix(byte over, byte under)
		{
			return (byte)Math.Round(over * alpha + under * (1 - alpha), MidpointRounding.AwayFromZero);
		}
	}

	// RGBA8888 in memory order, as one little-endian int
	private static int ToRgba(Color color) => color.R | (color.G << 8) | (color.B << 16) | (color.A << 24);
}
