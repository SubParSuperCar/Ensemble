using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Globals;

public static class Constants
{
	public const string ResourceScheme = "res://";
	public const string UserScheme = "user://";
	public const string HttpsScheme = "https://";

	public const string AppSettingsJson = "appsettings.json";
	public const string AppSettingsPath = ResourceScheme + AppSettingsJson;
	public const string UserAppSettingsPath = UserScheme + AppSettingsJson;

	public const string UserDataCfgPath = UserScheme + "user_data.cfg";
	public const string LogDir = UserScheme + "ensemble_logs/";

	public const string AssetsDir = ResourceScheme + "assets/";
	public const string BuildAssetsDir = ResourceScheme + "build_assets/";
	public const string ScenesDir = ResourceScheme + "scenes/";
	public const string ShadersDir = ResourceScheme + "shaders/";

	public const string GameIconPath = AssetsDir + "images/ensemble_icon_square_colored.png";

	public const string GitHubRepoPath = "SubParSuperCar/Ensemble";
	public const string GitHubRepoUrl = HttpsScheme + "github.com/" + GitHubRepoPath;

	public const int RegexMatchTimeoutMs = 100;

	public static readonly TimeSpan AnimationDuration = TimeSpan.FromSeconds(3 / 16d);

	/// <remarks>Also the multi-player handshake version: peers must match exactly.</remarks>
	public static string GameVersion => field ??= ProjectSettings.GetSetting("application/config/version").AsString();
}
