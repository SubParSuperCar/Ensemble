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
		var dir = ProjectSettings.GlobalizePath(ScriptConstants.LogDir);
		Directory.CreateDirectory(dir);

		var config = new ConfigurationBuilder()
			.SetBasePath(ProjectSettings.GlobalizePath(ScriptConstants.ResourceScheme))
			.AddJsonFile("appsettings.json", true, true)
			.Build();

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.ReadFrom.Configuration(config)
			.WriteTo.Sink(new GdSink())
			.WriteTo.Console(
				formatProvider: CultureInfo.InvariantCulture,
				outputTemplate:
				"[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
			.WriteTo.File(
				Path.Combine(dir, "serilog-.txt"),
				formatProvider: CultureInfo.InvariantCulture,
				flushToDiskInterval: TimeSpan.FromMinutes(1),
				rollingInterval: RollingInterval.Day,
				retainedFileCountLimit: 30,
				shared: true,
				outputTemplate:
				"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger();

		_loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.ClearProviders();
			builder.AddSerilog(Log.Logger);
		});
	}

	public override void _ExitTree()
	{
		_loggerFactory?.Dispose();
		Log.CloseAndFlush();
	}
}
