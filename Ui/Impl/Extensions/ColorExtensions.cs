using Avalonia.Media;

namespace EnsembleRoot.Ui.Impl.Extensions;

public static class ColorExtensions
{
	private const double VibrantChroma = 0.13;
	private const double MaxChromaBoost = 2d;
	private const double GamutTolerance = 1e-4;

	private static readonly double WhiteBlackCrossoverLuminance = Math.Sqrt(1.05 * 0.05) - 0.05;

	extension(Color source)
	{
		// ReSharper disable once MemberCanBePrivate.Global
		public double RelativeLuminance =>
			0.2126 * Linearize(source.R) + 0.7152 * Linearize(source.G) + 0.0722 * Linearize(source.B);

		public Color ContrastingForeground =>
			source.RelativeLuminance < WhiteBlackCrossoverLuminance ? Colors.White : Colors.Black;

		public Color ToVibrant(double minLightness, double maxLightness)
		{
			var (lightness, a, b) = ToOklab(Linearize(source.R), Linearize(source.G), Linearize(source.B));
			var chroma = Math.Sqrt(a * a + b * b);
			var hue = Math.Atan2(b, a);

			lightness = Math.Clamp(lightness, minLightness, maxLightness);
			chroma = Math.Max(chroma, Math.Min(chroma * MaxChromaBoost, VibrantChroma));

			if (!IsInGamut(lightness, chroma, hue))
			{
				var low = 0d;

				while (chroma - low > GamutTolerance)
				{
					var mid = (low + chroma) / 2;

					if (IsInGamut(lightness, mid, hue))
						low = mid;
					else
						chroma = mid;
				}

				chroma = low;
			}

			var (red, green, blue) = ToLinearRgb(lightness, chroma, hue);

			return Color.FromRgb(Encode(red), Encode(green), Encode(blue));
		}
	}

	// https://bottosson.github.io/posts/oklab/
	private static (double L, double A, double B) ToOklab(double red, double green, double blue)
	{
		var l = Math.Cbrt(0.4122214708 * red + 0.5363325363 * green + 0.0514459929 * blue);
		var m = Math.Cbrt(0.2119034982 * red + 0.6806995451 * green + 0.1073969566 * blue);
		var s = Math.Cbrt(0.0883024619 * red + 0.2817188376 * green + 0.6299787005 * blue);

		return (
			0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
			1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
			0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
	}

	private static (double R, double G, double B) ToLinearRgb(double lightness, double chroma, double hue)
	{
		var a = chroma * Math.Cos(hue);
		var b = chroma * Math.Sin(hue);

		var l = Math.Pow(lightness + 0.3963377774 * a + 0.2158037573 * b, 3);
		var m = Math.Pow(lightness - 0.1055613458 * a - 0.0638541728 * b, 3);
		var s = Math.Pow(lightness - 0.0894841775 * a - 1.2914855480 * b, 3);

		return (
			4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
			-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
			-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s);
	}

	private static bool IsInGamut(double lightness, double chroma, double hue)
	{
		var (red, green, blue) = ToLinearRgb(lightness, chroma, hue);

		return IsInRange(red) && IsInRange(green) && IsInRange(blue);

		static bool IsInRange(double value)
		{
			return value is >= -GamutTolerance and <= 1 + GamutTolerance;
		}
	}

	private static double Linearize(byte channel)
	{
		var value = channel / 255d;
		return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
	}

	private static byte Encode(double value)
	{
		var encoded = value <= 0.0031308 ? 12.92 * value : 1.055 * Math.Pow(value, 1 / 2.4) - 0.055;
		return (byte)Math.Round(255 * Math.Clamp(encoded, 0, 1), MidpointRounding.ToEven);
	}
}
