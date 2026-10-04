using System.Security.Cryptography;
using EnsembleRoot.Autoloading;
using EnsembleRoot.Common.Logging;
using EnsembleRoot.Common.Utils;
using EnsembleRoot.Scripts.Logging.Impl;
using Godot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using TinyDialogsNet;
using FileAccess = Godot.FileAccess;

namespace EnsembleRoot.Scripts.Logging;

[GlobalClass]
[Autoload(Order = AutoloadOrder.First, FailurePolicy = AutoloadFailurePolicy.AskUser)]
public partial class Logger : Node, IAutoload
{
	private const string LogFileNameTemplate = "ensemble-serilog-.json";

	private const string AppSettingsSection = "app_settings";
	private const string DefaultsHashKey = "defaults_sha256";

	private ILoggerFactory? _factory;

	public static ILoggerFactory? Factory { get; private set; }

	public void Initialize()
	{
		LoggerConfiguration loggerConfig;

		string? logDir = null;
		string? syncNote = null;
		Exception? userFailure = null;
		Exception? failure = null;

		try
		{
			logDir = ProjectSettings.GlobalizePath(LogDir);
			Directory.CreateDirectory(logDir);

			try
			{
				var configuration = BuildConfiguration(logDir, true, out syncNote);
				loggerConfig = CreateBaseConfig().ReadFrom.Configuration(configuration);
			}
			catch (Exception exception)
			{
				userFailure = exception;
				loggerConfig = CreateBaseConfig().ReadFrom.Configuration(BuildConfiguration(logDir, false, out _));
			}
		}
		catch (Exception exception)
		{
			failure = exception;
			loggerConfig = CreateBaseConfig();
		}

		Log.Logger = loggerConfig.CreateLogger();

		_factory = LoggerFactory.Create(static builder =>
		{
			builder.ClearProviders();
			builder.AddSerilog(Log.Logger);
		});
		Factory = _factory;

		if (failure is null)
			Log.Information(
				"Writing {Class} log files to {Directory} with name template {NameTemplate}",
				nameof(Serilog),
				logDir,
				LogFileNameTemplate);
		else
			Log.Error(failure, "Could not build {Class} configuration", nameof(Serilog));

		if (syncNote is not null)
			Log.Information("{Note}", syncNote);

		if (userFailure is not null)
			Log.Warning(
				userFailure,
				"Could not apply {Path}; using the default {File} instead",
				UserAppSettingsPath,
				AppSettingsJson);
	}

	public override void _ExitTree()
	{
		Log.Debug("Closing and flushing logger...");

		_factory?.Dispose();
		if (ReferenceEquals(Factory, _factory))
			Factory = null;

		Log.CloseAndFlush();
	}

	private static LoggerConfiguration CreateBaseConfig() =>
		new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.Enrich.With(new LogEnricher())
			.WriteTo.Sink(new LogSink())
			.WriteTo.Sink(new VolatileLogHistorySink());

	private static IConfiguration BuildConfiguration(string logDir, bool useUserCopy, out string? syncNote)
	{
		syncNote = null;
		var bytes = useUserCopy ? ReadUserAppSettings(out syncNote) : ReadAllBytesOrThrow(AppSettingsPath);

		var configBuilder = new ConfigurationBuilder();

		configBuilder.AddJsonStream(new MemoryStream(bytes));
		configBuilder.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
		{
			["Serilog:WriteTo:0:Args:path"] = Path.Combine(logDir, LogFileNameTemplate)
		});

		return configBuilder.Build();
	}

	private static byte[] ReadUserAppSettings(out string? syncNote)
	{
		syncNote = null;

		try
		{
			syncNote = SyncUserAppSettings();
			return ReadAllBytesOrThrow(UserAppSettingsPath);
		}
		catch
		{
			return ReadAllBytesOrThrow(AppSettingsPath);
		}
	}

	/// <summary>
	///     Copies the embedded defaults if the user copy is missing. If the embedded defaults changed since the user
	///     copy was last synced and the user copy differs from them, asks once whether to replace it, keeping a backup.
	/// </summary>
	private static string? SyncUserAppSettings()
	{
		var defaults = ReadAllBytesOrThrow(AppSettingsPath);
		var defaultsHash = Convert.ToHexString(SHA256.HashData(defaults));

		var syncedHash = UserData.GetValue(AppSettingsSection, DefaultsHashKey, string.Empty).AsString();
		string? note = null;

		if (!FileAccess.FileExists(UserAppSettingsPath))
		{
			Copy(AppSettingsPath, UserAppSettingsPath);
			note = $"Created {UserAppSettingsPath} from the defaults";
		}
		else if (
			!string.Equals(syncedHash, defaultsHash, StringComparison.Ordinal) &&
			!ReadAllBytesOrThrow(UserAppSettingsPath).AsSpan().SequenceEqual(defaults))
		{
			if (ShouldReplaceUserAppSettings())
			{
				Copy(UserAppSettingsPath, UserAppSettingsPath + ".bak");
				Copy(AppSettingsPath, UserAppSettingsPath);

				note = $"Replaced {UserAppSettingsPath} with the new defaults (backup: {UserAppSettingsPath}.bak)";
			}
			else
				note = $"Kept {UserAppSettingsPath} despite newer defaults";
		}

		if (!string.Equals(syncedHash, defaultsHash, StringComparison.Ordinal))
			UserData.SetValue(AppSettingsSection, DefaultsHashKey, defaultsHash);

		return note;
	}

	private static bool ShouldReplaceUserAppSettings()
	{
		if (Main.IsHeadlessServer)
			return false;

		try
		{
			var response = TinyDialogs.MessageBox(
				"Ensemble Settings Defaults Changed",
				Main.SanitizeMessageBoxBody(
					$"This version of Ensemble ships a newer default {AppSettingsJson} than the one in your user " +
					"data directory, which has been edited or is from an older version.\n\n" +
					"Replace yours with the new defaults? Your current file will be kept as a .bak backup.\n" +
					"You will not be asked again until the defaults change."),
				MessageBoxDialogType.YesNo,
				MessageBoxIconType.Question,
				MessageBoxButton.No);

			return response is MessageBoxButton.Yes;
		}
		catch
		{
			return false;
		}
	}

	private static void Copy(string from, string to)
	{
		var result = DirAccess.CopyAbsolute(from, to);
		if (result is not Error.Ok)
			throw new IOException($"Could not copy {from} to {to}: {result}.");
	}

	private static byte[] ReadAllBytesOrThrow(string path)
	{
		using var file =
			FileAccess.Open(path, FileAccess.ModeFlags.Read) ??
			throw new IOException($"Could not open {path} for reading: {FileAccess.GetOpenError()}.");

		return file.GetBuffer((long)file.GetLength());
	}
}
