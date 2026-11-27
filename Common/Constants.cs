using System.Reflection;
using EnsembleRoot.Common.Utils;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Common;

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
	public const string CreationsDir = UserScheme + "creations/";
	public const string ScriptsDir = UserScheme + "scripts/";

	public const string AssetsDir = ResourceScheme + "assets/";
	public const string BuildAssetsDir = ResourceScheme + "build_assets/";
	public const string ScenesDir = ResourceScheme + "scenes/";
	public const string ShadersDir = ResourceScheme + "shaders/";

	public const string GameIconPath = AssetsDir + "images/ensemble_icon.png";

	public const string GitHubRepoPath = "SubParSuperCar/Ensemble";
	public const string GitHubRepoUrl = HttpsScheme + "github.com/" + GitHubRepoPath;
	public const string ItchPageUrl = HttpsScheme + "subparsupercar.itch.io/ensemble";

	public const int RegexMatchTimeoutMs = 100;

	/// <summary>Meters per grid unit.</summary>
	public const float GridToWorldScale = 0.5f;

	/// <summary>
	///     The narrowest and widest gaps (about 1 and 2 mm) by which an asset's colliders may sit inside its cell, so
	///     the faces they inset never weld. Binary, just under the millimeters assets are authored in.
	/// </summary>
	public const float MinColliderInset = 1 / 1024f;

	/// <inheritdoc cref="MinColliderInset" />
	public const float MaxColliderInset = 1 / 512f;

	/// <summary>
	///     The world-space distance (about 0.5 mm) within which geometry counts as touching, for placement and welding:
	///     half the narrowest collider inset, so inset faces never touch.
	/// </summary>
	public const float Epsilon = MinColliderInset / 2;

	private const string UiSection = "ui";
	private const string AreAnimationsOnKey = "are_animations_on";

	/// <summary>
	///     Propagated through the tree once the game has decided to quit, past the engine's own notifications. The
	///     window's close request only asks, as the player may cancel it.
	/// </summary>
	public const int NotificationQuitting = 1 << 20;

	private static TimeSpan? _animationDuration;

	/// <summary>
	///     How long the UI's transitions take, or none while they're turned off, as of this launch: styles read it
	///     once, as they load.
	/// </summary>
	public static TimeSpan AnimationDuration =>
		_animationDuration ??= AreAnimationsOn ? TimeSpan.FromSeconds(3 / 16d) : TimeSpan.Zero;

	/// <summary>Whether the UI animates, remembered across launches; a change takes effect on the next one.</summary>
	public static bool AreAnimationsOn
	{
		get => UserData.GetValue(UiSection, AreAnimationsOnKey, true).AsBool();
		set => UserData.SetValue(UiSection, AreAnimationsOnKey, value);
	}

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

	/// <summary>One transfer channel per unreliable stream, since a channel drops what arrives out of order.</summary>
	public static class NetworkChannels
	{
		public const int Characters = 1;
		public const int Creations = 2;
		public const int Input = 3;
	}
}
