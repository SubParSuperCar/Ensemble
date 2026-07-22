using System.Globalization;

namespace Root.Globals;

public static class Util
{
	public static string FormatBytes(ulong bytes)
	{
		string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];

		double value = bytes;
		var unit = 0;

		while (value >= 1024 && unit < units.Length - 1)
		{
			value /= 1024;
			unit++;
		}

		return string.Create(CultureInfo.InvariantCulture, $"{value:F3} {units[unit]}");
	}
}
