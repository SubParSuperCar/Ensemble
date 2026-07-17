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
				outputTemplate:
				"[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
				formatProvider: CultureInfo.InvariantCulture)
			.WriteTo.File(
				Path.Combine(dir, "serilog-.txt"),
				outputTemplate:
				"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
				formatProvider: CultureInfo.InvariantCulture,
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
	}

	public override void _ExitTree()
	{
		_loggerFactory?.Dispose();
		Log.CloseAndFlush();
	}
}
