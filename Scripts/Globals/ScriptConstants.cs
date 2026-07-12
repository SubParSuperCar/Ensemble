// ReSharper disable MemberCanBePrivate.Global

namespace Root.Scripts.Globals;

public static class ScriptConstants
{
	public const string UserScheme = "user://";
	public const string ResourceScheme = "res://";

	// ReSharper disable once UnusedMember.Global
	public const string UserDataCfgPath = UserScheme + "user_data.cfg";
	public const string AssetsDir = ResourceScheme + "BuildAssets";
}
