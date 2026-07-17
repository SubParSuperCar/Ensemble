using System.Globalization;
using Godot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Root.Scripts.Globals;
using Root.Scripts.StdOut.Impl;
using Serilog;
using Serilog.Templates;

namespace Root.Scripts.StdOut;

public partial class Logger : Node
{
	private ILoggerFactory? _loggerFactory;

	public override void _EnterTree()
	{
		var logDir = ProjectSettings.GlobalizePath(ScriptConstants.LogDir);
		Directory.CreateDirectory(logDir);

		var config = new ConfigurationBuilder()
			.SetBasePath(ProjectSettings.GlobalizePath(ScriptConstants.ResourceScheme))
			.AddJsonFile("appsettings.json", true, true)
			.Build();

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.ReadFrom.Configuration(config)
			.Enrich.With(new LogEnricher())
			.WriteTo.Sink(new LogSink())
			.WriteTo.Console(
				outputTemplate:
				"[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
				formatProvider: CultureInfo.InvariantCulture)
			.WriteTo.File(
				new ExpressionTemplate(
					"{ {@t: @t, @l: @l, @m: @m, @x: @x, ..@p} }\n",
					CultureInfo.InvariantCulture
				),
				Path.Combine(logDir, "serilog-.json"),
				shared: true,
				flushToDiskInterval: TimeSpan.FromSeconds(2),
				rollingInterval: RollingInterval.Day,
				rollOnFileSizeLimit: true,
				retainedFileCountLimit: 30,
				retainedFileTimeLimit: TimeSpan.FromDays(7))
			.CreateLogger();

		_loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.ClearProviders();
			builder.AddSerilog(Log.Logger);
		});

		Log.Debug("Created logger. Logging to directory: {Directory}", logDir);
	}

	public override void _ExitTree()
	{
		Log.Debug("Flushing & closing logger...");

		_loggerFactory?.Dispose();
		Log.CloseAndFlush();
	}
}
