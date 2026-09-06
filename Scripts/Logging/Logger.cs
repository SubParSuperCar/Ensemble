using Godot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Root.Autoloading;
using Root.Common.Logging;
using Root.Scripts.Logging.Impl;
using Serilog;
using FileAccess = Godot.FileAccess;

namespace Root.Scripts.Logging;

[GlobalClass]
[Autoload(Order = sbyte.MinValue, FailurePolicy = AutoloadFailurePolicy.AskUser)]
public partial class Logger : Node, IAutoload
{
	private const string LogFileNameTemplate = "serilog-.json";

	private ILoggerFactory? _loggerFactory;

	public void Initialize()
	{
		var loggerConfig = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.Enrich.With(new LogEnricher())
			.WriteTo.Sink(new LogSink())
			.WriteTo.Sink(new VolatileLogHistorySink());

		string? logDir = null;
		Exception? failure = null;

		try
		{
			logDir = ProjectSettings.GlobalizePath(LogDir);
			Directory.CreateDirectory(logDir);

			loggerConfig = loggerConfig.ReadFrom.Configuration(BuildConfiguration(logDir));
		}
		catch (Exception exception)
		{
			failure = exception;
		}

		Log.Logger = loggerConfig.CreateLogger();

		_loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.ClearProviders();
			builder.AddSerilog(Log.Logger);
		});

		if (failure is null)
			Log.Information("Writing {Class} logs to: {Directory}", nameof(Serilog), logDir);
		else
			Log.Error(failure, "Could not build {Class} configuration.", nameof(Serilog));
	}

	public override void _ExitTree()
	{
		Log.Debug("Closing and flushing logger...");

		_loggerFactory?.Dispose();
		Log.CloseAndFlush();
	}

	private static IConfiguration BuildConfiguration(string logDir)
	{
		byte[] bytes;

		try
		{
			MakeUserAppSettingsIfMissing();
			bytes = ReadAllBytesOrThrow(UserAppSettingsPath);
		}
		catch
		{
			bytes = ReadAllBytesOrThrow(AppSettingsPath);
		}

		var configBuilder = new ConfigurationBuilder();

		configBuilder.AddJsonStream(new MemoryStream(bytes));
		configBuilder.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
		{ ["Serilog:WriteTo:0:Args:path"] = Path.Combine(logDir, LogFileNameTemplate) });

		return configBuilder.Build();
	}

	private static void MakeUserAppSettingsIfMissing()
	{
		if (FileAccess.FileExists(UserAppSettingsPath))
			return;

		var result = DirAccess.CopyAbsolute(AppSettingsPath, UserAppSettingsPath);
		if (result is not Error.Ok)
			throw new IOException($"Could not copy {AppSettingsPath} to {UserAppSettingsPath} ({result})");
	}

	private static byte[] ReadAllBytesOrThrow(string path)
	{
		using var file =
			FileAccess.Open(path, FileAccess.ModeFlags.Read) ??
			throw new IOException($"Could not open for reading: {path} ({FileAccess.GetOpenError()})");

		return file.GetBuffer((long)file.GetLength());
	}
}
