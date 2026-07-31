// ReSharper disable MemberCanBePrivate.Global

namespace Root.Common.Globals;

public static class CommonConstants
{
	public const string UserScheme = "user://";
	public const string ResourceScheme = "res://";

	public const string AppSettingsPath = ResourceScheme + "appsettings.json";
	public const string LogDir = UserScheme + "logs/";
	public const string UserDataCfgPath = UserScheme + "user_data.cfg";
	public const string GameIconPath = ResourceScheme + "assets/images/ensemble_icon_square_colored.png";
	public const string BuildAssetsDir = ResourceScheme + "build_assets/";
}
