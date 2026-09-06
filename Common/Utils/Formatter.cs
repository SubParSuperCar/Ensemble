using System.Globalization;

namespace Root.Common.Utils;

public static class Formatter
{
	private static readonly string[] Units = ["B", "KiB", "MiB", "GiB", "TiB"];

	public static string FormatBytes(ulong bytes)
	{
		double value = bytes;
		var unit = 0;

		while (value >= 1024 && unit < Units.Length - 1)
		{
			value /= 1024;
			unit++;
		}

		return $"{value.ToString(unit is 0 ? "F0" : "F3", CultureInfo.InvariantCulture)} {Units[unit]}";
	}
}
