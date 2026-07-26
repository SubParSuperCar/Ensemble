using System.Globalization;
using Godot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Root.Common.Logging;
using Root.Scripts.Globals;
using Root.Scripts.Logger.Impl;
using Serilog;
using Serilog.Templates;
using FileAccess = Godot.FileAccess;

namespace Root.Scripts.Logger;

public partial class Logger : Node
{
	private ILoggerFactory? _loggerFactory;

	public override void _EnterTree()
	{
		var logDir = ProjectSettings.GlobalizePath(ScriptConstants.LogDir);
		Directory.CreateDirectory(logDir);

		var configBuilder = new ConfigurationBuilder();

		using var file = FileAccess.Open(ScriptConstants.AppSettingsPath, FileAccess.ModeFlags.Read);

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
			.WriteTo.Sink(new LogHistorySinkVolatile())
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

		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

		Log.Debug("Created logger. Logging to directory: {Directory}", logDir);
	}

	public override void _ExitTree()
	{
		Log.Debug("Closing & flushing logger...");

		AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
		TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

		_loggerFactory?.Dispose();
		Log.CloseAndFlush();
	}

	private static void OnUnhandledException(object? _, UnhandledExceptionEventArgs e)
	{
		Log.Fatal("Unhandled exception:\n{Exception}", e.ExceptionObject as Exception);
		Log.CloseAndFlush();
	}

	private static void OnUnobservedTaskException(object? _, UnobservedTaskExceptionEventArgs e)
	{
		Log.Error<Exception>("Unobserved task exception:\n{Exception}", e.Exception);
		e.SetObserved();
	}
}
