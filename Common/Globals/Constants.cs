namespace Root.Common.Globals;

public static class Constants
{
	// ReSharper disable once UnusedMember.Local
	private const string ResourceScheme = "res://";
	private const string UserScheme = "user://";

	public const string AppSettingsPath = UserScheme + "appsettings.json";
	public const string UserDataCfgPath = UserScheme + "user_data.cfg";
	public const string LogDir = UserScheme + "logs/";
}
