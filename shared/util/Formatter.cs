using System.Globalization;

namespace Root.Shared.Util;

public static class Formatter
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

		return $"{value.ToString(unit is 0 ? "F0" : "F3", CultureInfo.InvariantCulture)} {units[unit]}";
	}
}
