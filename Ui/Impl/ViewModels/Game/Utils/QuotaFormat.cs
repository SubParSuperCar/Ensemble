using System.Globalization;

namespace EnsembleRoot.Ui.Impl.ViewModels.Utils;

internal static class QuotaFormat
{
	public static string Fraction(int count, int max) =>
		string.Create(
			CultureInfo.InvariantCulture,
			$"{count} / {(max is Unlimited ? "\u221E" : max.ToString(CultureInfo.InvariantCulture))}");
}
