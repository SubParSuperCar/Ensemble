using System.Globalization;
using Godot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Root.Autoloading;
using Root.Scripts.Logging.Impl;
using Serilog;
using Serilog.Templates;
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

		using var file = FileAccess.Open(AppSettingsPath, FileAccess.ModeFlags.Read);

		if (file is not null)
		{
			var bytes = file.GetBuffer((long)file.GetLength());
			configBuilder.AddJsonStream(new MemoryStream(bytes));
		}

		var config = configBuilder.Build();

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.ReadFrom.Configuration(config)
			.Enrich.With(new LogEnricher())
			.WriteTo.Sink(new LogSink())
			.WriteTo.Console(
				outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
				formatProvider: CultureInfo.InvariantCulture)
			.WriteTo.File(
				new ExpressionTemplate(
					"{ {@t: @t, @l: @l, @m: @m, @x: @x, ..@p} }\n",
					CultureInfo.InvariantCulture
				),
				Path.Combine(logDir, "log-.json"),
				flushToDiskInterval: TimeSpan.FromSeconds(2),
				rollingInterval: RollingInterval.Day,
				rollOnFileSizeLimit: true,
				retainedFileCountLimit: 30,
				retainedFileTimeLimit: TimeSpan.FromDays(30))
			.CreateLogger();

		_loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.ClearProviders();
			builder.AddSerilog(Log.Logger);
		});
	}

	public override void _ExitTree()
	{
		Log.Debug("Closing and flushing {Logger}...", Log.Logger);

		_loggerFactory?.Dispose();
		Log.CloseAndFlush();
	}
}
