using System.Globalization;
using EnsembleRoot.Common.Utils;

namespace EnsembleRoot.Common.Text;

/// <summary>
///     The culture the game shows and reads numbers and dates in (a decimal comma or point, say): the system's unless
///     chosen, remembered across launches.
/// </summary>
public static class AppCulture
{
	private const string Section = "locale";
	private const string CultureKey = "culture";

	// Neither is a culture's name, which the invariant culture's empty one would be
	private const string SystemValue = "system";
	private const string InvariantValue = "invariant";

	private static readonly CultureInfo SystemCulture = CultureInfo.CurrentCulture;

	/// <summary>Sets the culture, or goes back to the system's with null, and remembers it.</summary>
	public static void Set(CultureInfo? culture)
	{
		Apply(culture ?? SystemCulture);

		var value = culture is null ? SystemValue : culture.Name is "" ? InvariantValue : culture.Name;
		UserData.SetValue(Section, CultureKey, value);
	}

	/// <summary>
	///     Finds a culture by name (e.g., <c>en-US</c>), or the invariant one, which formats like English whatever the
	///     language, by <c>invariant</c>.
	/// </summary>
	/// <exception cref="CultureNotFoundException">No culture has the name.</exception>
	public static CultureInfo Find(string name) =>
		string.Equals(name, InvariantValue, StringComparison.OrdinalIgnoreCase)
			? CultureInfo.InvariantCulture
			: CultureInfo.GetCultureInfo(name, true);

	/// <summary>Applies the remembered culture; the user data file is hand-editable, so it may name none.</summary>
	public static void Restore()
	{
		var name = UserData.GetValue(Section, CultureKey, SystemValue).AsString();

		if (string.Equals(name, SystemValue, StringComparison.OrdinalIgnoreCase))
			return;

		try
		{
			Apply(Find(name));
		}
		// Before the log starts, as numbers and dates are formatted from the start
		catch (CultureNotFoundException)
		{
			Console.WriteLine($"Ignored the remembered culture {name}, which no longer exists");
		}
	}

	// The default reaches every thread that hasn't set its own, including the thread pool's
	private static void Apply(CultureInfo culture)
	{
		CultureInfo.CurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;
	}
}
