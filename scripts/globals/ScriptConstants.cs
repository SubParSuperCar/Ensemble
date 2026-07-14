namespace Root.Scripts.Globals;

public static class ScriptConstants
{
	// ReSharper disable once MemberCanBePrivate.Global
	public const string UserScheme = "user://";
	public const string ResourceScheme = "res://";

	public const string LogDir = UserScheme + "logs";
	public const string UserDataCfgPath = UserScheme + "user_data.cfg";
	public const string BuildAssetsDir = ResourceScheme + "build_assets";
}
