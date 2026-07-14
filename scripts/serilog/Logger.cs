using System.Globalization;
using Godot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Root.Scripts.Globals;
using Root.Scripts.StdOut.Impl;
using Serilog;

namespace Root.Scripts.StdOut;

public partial class Logger : Node
{
	private ILoggerFactory? _loggerFactory;

	public override void _EnterTree()
	{
		var logDirectory = ProjectSettings.GlobalizePath("user://logs");
		Directory.CreateDirectory(logDirectory);

		var configuration = new ConfigurationBuilder()
			.SetBasePath(ProjectSettings.GlobalizePath(ScriptConstants.ResourceScheme))
			.AddJsonFile("appsettings.json", true, true)
			.Build();

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.ReadFrom.Configuration(configuration)
			.WriteTo.Sink(new GdSink())
			.WriteTo.Console(
				formatProvider: CultureInfo.InvariantCulture,
				outputTemplate:
				"[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
			.WriteTo.File(
				Path.Combine(logDirectory, "serilog-.txt"),
				formatProvider: CultureInfo.InvariantCulture,
				rollingInterval: RollingInterval.Day,
				retainedFileCountLimit: 30,
				shared: true,
				outputTemplate:
				"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger();

		_loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.ClearProviders();
			builder.AddSerilog(Log.Logger, false);
		});
	}

	public override void _ExitTree()
	{
		_loggerFactory?.Dispose();
		Log.CloseAndFlush();
	}
}
