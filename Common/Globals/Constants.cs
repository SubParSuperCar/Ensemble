namespace Root.Common.Globals;

public static class Constants
{
	public const string ResourceScheme = "res://";
	public const string UserScheme = "user://";

	public const string AppSettingsPath = UserScheme + "appsettings.json";
	public const string UserDataCfgPath = UserScheme + "user_data.cfg";

	public const string LogDir = UserScheme + "logs/";

	public const string AssetsDir = ResourceScheme + "assets/";
	public const string ScenesDir = ResourceScheme + "scenes/";
	public const string BuildAssetsDir = ResourceScheme + "build_assets/";

	public const string GameIconPath = AssetsDir + "/images/ensemble_icon_square_colored.png";
}
