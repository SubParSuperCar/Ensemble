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
		var logDir = ProjectSettings.GlobalizePath(LogDir);
		Directory.CreateDirectory(logDir);

		var configBuilder = new ConfigurationBuilder();
		var configFileLoaded = false;

		if (FileAccess.FileExists(AppSettingsPath))
		{
			using var file = FileAccess.Open(AppSettingsPath, FileAccess.ModeFlags.Read);

			if (file is not null)
			{
				var bytes = file.GetBuffer((long)file.GetLength());
				configBuilder.AddJsonStream(new MemoryStream(bytes));

				configBuilder.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
				{ ["Serilog:WriteTo:1:Args:path"] = Path.Combine(logDir, "serilog-.json") });

				configFileLoaded = true;
			}
		}

		var config = configBuilder.Build();

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.ReadFrom.Configuration(config)
			.Enrich.With(new LogEnricher())
			.WriteTo.Sink(new LogSink())
			.WriteTo.Sink(new VolatileLogHistorySink())
			.CreateLogger();

		_loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.ClearProviders();
			builder.AddSerilog(Log.Logger);
		});

		if (configFileLoaded)
			Log.Information("Writing {Class} logs to: {Directory}", nameof(Serilog), logDir);
		else
			Log.Warning("{Class} config file not loaded from: {Path}", nameof(Serilog), AppSettingsPath);
	}

	public override void _ExitTree()
	{
		Log.Debug("Closing and flushing logger...");

		_loggerFactory?.Dispose();
		Log.CloseAndFlush();
	}
}
