namespace Root.Common.Globals;

public static class Constants
{
	public const string ResourceScheme = "res://";
	public const string UserScheme = "user://";

	public const string AppSettingsJson = "appsettings.json";
	public const string AppSettingsPath = ResourceScheme + AppSettingsJson;
	public const string UserAppSettingsPath = UserScheme + AppSettingsJson;

	public const string UserDataCfgPath = UserScheme + "user_data.cfg";

	public const string LogDir = UserScheme + "ensemble_logs/";

	public const string AssetsDir = ResourceScheme + "assets/";
	public const string ScenesDir = ResourceScheme + "scenes/";
	public const string BuildAssetsDir = ResourceScheme + "build_assets/";

	public const string GameIconPath = AssetsDir + "images/ensemble_icon_square_colored.png";
}
