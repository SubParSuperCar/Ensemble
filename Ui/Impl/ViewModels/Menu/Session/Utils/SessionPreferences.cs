using EnsembleRoot.Common.Utils;

namespace EnsembleRoot.Ui.Impl.ViewModels.Utils;

internal static class SessionPreferences
{
	private const string Section = "session";
	private const string DisplayNameKey = "display_name";

	public static string DisplayName
	{
		get => UserData.GetValue(Section, DisplayNameKey, string.Empty).AsString();
		set => UserData.SetValue(Section, DisplayNameKey, value);
	}
}
