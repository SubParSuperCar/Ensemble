using System.Reflection;
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
	public const string HostKeyPath = UserScheme + "host.key";
	public const string HostCertificatePath = UserScheme + "host.crt";
	public const string LogDir = UserScheme + "ensemble_logs/";
	public const string ChatLogDir = UserScheme + "chat_logs/";

	public const string AssetsDir = ResourceScheme + "assets/";
	public const string BuildAssetsDir = ResourceScheme + "build_assets/";
	public const string ScenesDir = ResourceScheme + "scenes/";
	public const string ShadersDir = ResourceScheme + "shaders/";

	public const string GameIconPath = AssetsDir + "images/ensemble_icon_square_colored.png";

	public const string GitHubRepoPath = "SubParSuperCar/Ensemble";
	public const string GitHubRepoUrl = HttpsScheme + "github.com/" + GitHubRepoPath;

	public const int RegexMatchTimeoutMs = 100;

	/// <summary>
	///     The world-space distance (about 0.5 mm) within which geometry counts as touching, for placement and welding.
	/// </summary>
	public const float Epsilon = 1 / 2048f;

	public static readonly TimeSpan AnimationDuration = TimeSpan.FromSeconds(3 / 16d);

	/// <remarks>Also the multi-player handshake version: peers must match exactly.</remarks>
	public static string GameVersion => field ??= ProjectSettings.GetSetting("application/config/version").AsString();

	/// <summary>The source commit, which the SDK appends to the informational version (e.g., 1.0.0+abc123).</summary>
	public static string GameCommit { get; } =
		typeof(Constants).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
			.Split('+') is [_, var commit]
			? commit
			: "Unknown";

	/// <summary>Mirrors project.godot's 3D physics layer names, as bits for collision layers and masks.</summary>
	public static class PhysicsLayers
	{
		public const uint World = 1 << 0;
		public const uint Character = 1 << 1;
		public const uint StaticInstances = 1 << 2;
		public const uint DynamicInstances = 1 << 3;
	}
}
