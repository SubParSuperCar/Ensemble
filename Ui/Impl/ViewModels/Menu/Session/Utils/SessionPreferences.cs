using EnsembleRoot.Common.Utils;

namespace EnsembleRoot.Ui.Impl.ViewModels.Utils;

internal static class SessionPreferences
{
	private const string Section = "session";
	private const string DisplayNameKey = "display_name";
	private const string IsUpnpEnabledKey = "upnp_enabled";

	public static string DisplayName
	{
		get => UserData.GetValue(Section, DisplayNameKey, string.Empty).AsString();
		set => UserData.SetValue(Section, DisplayNameKey, value);
	}

	public static bool IsUpnpEnabled
	{
		get => UserData.GetValue(Section, IsUpnpEnabledKey, true).AsBool();
		set => UserData.SetValue(Section, IsUpnpEnabledKey, value);
	}
}
