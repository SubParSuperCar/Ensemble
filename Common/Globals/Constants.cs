namespace Root.Common.Globals;

// Generic codebase-wide constant information such as Godot path schemes, etc.
// We could consider enumerating the game's name (Ensemble) here, but it's not that important.
// We'll ultimately not be able to use it everywhere like in AXAML, license texts, docs, etc.
public static class Constants
{
	// ReSharper disable once UnusedMember.Local
	private const string ResourceScheme = "res://";
	private const string UserScheme = "user://";

	public const string AppSettingsPath = UserScheme + "appsettings.json";
	public const string UserDataCfgPath = UserScheme + "user_data.cfg";
	public const string LogDir = UserScheme + "logs/";
}
