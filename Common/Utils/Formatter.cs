using System.Globalization;

namespace Root.Common.Utils;

public static class Formatter
{
	private static readonly string[] Units = ["B", "KiB", "MiB", "GiB", "TiB"];

	public static string FormatBytes(ulong bytes)
	{
		double value = bytes;
		var unitIndex = 0;

		while (value >= 1024 && unitIndex < Units.Length - 1)
		{
			value /= 1024;
			unitIndex++;
		}

		return $"{value.ToString(unitIndex is 0 ? "F0" : "F3", CultureInfo.InvariantCulture)} {Units[unitIndex]}";
	}
}
