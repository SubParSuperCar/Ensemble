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
	private ILoggerFactory? _loggerFactory;

	public void Initialize()
	{
		var loggerConfig = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.Enrich.With(new LogEnricher())
			.WriteTo.Sink(new LogSink())
			.WriteTo.Sink(new VolatileLogHistorySink());

		string? logDir = null;
		Exception? exception = null;

		try
		{
			logDir = ProjectSettings.GlobalizePath(LogDir);
			Directory.CreateDirectory(logDir);

			var config = BuildConfiguration(logDir);
			loggerConfig = loggerConfig.ReadFrom.Configuration(config);
		}
		catch (Exception ex)
		{
			exception = ex;
		}

		Log.Logger = loggerConfig.CreateLogger();

		_loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.ClearProviders();
			builder.AddSerilog(Log.Logger);
		});

		if (exception is null)
			Log.Information("Writing {Class} logs to: {Directory}", nameof(Serilog), logDir);
		else
			Log.Error(exception, "Could not build {Class} configuration.", nameof(Serilog));
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

			using var file = OpenReadOrThrow(UserAppSettingsPath);
			bytes = file.GetBuffer((long)file.GetLength());
		}
		catch
		{
			using var file = OpenReadOrThrow(AppSettingsPath);
			bytes = file.GetBuffer((long)file.GetLength());
		}

		var configBuilder = new ConfigurationBuilder();
		configBuilder.AddJsonStream(new MemoryStream(bytes));
		configBuilder.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
		{ ["Serilog:WriteTo:0:Args:path"] = Path.Combine(logDir, "serilog-.json") });

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

	private static FileAccess OpenReadOrThrow(string path)
	{
		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		return file ?? throw new IOException($"Could not open for reading: {path} ({FileAccess.GetOpenError()})");
	}
}
