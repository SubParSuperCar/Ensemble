using System.Globalization;
using Godot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Root.Common.Globals;
using Root.Common.Logging;
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
		var logDir = ProjectSettings.GlobalizePath(CommonConstants.LogDir);
		Directory.CreateDirectory(logDir);

		var configBuilder = new ConfigurationBuilder();

		using var file = FileAccess.Open(CommonConstants.AppSettingsPath, FileAccess.ModeFlags.Read);

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
				Path.Combine(logDir, "log-.json"),
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

		Log.Debug("Logger initialized; writing logs to {LogDir}", logDir);
	}

	public override void _ExitTree()
	{
		Log.Debug("Closing and flushing logger...");

		AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
		TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

		_loggerFactory?.Dispose();
		Log.CloseAndFlush();
	}

	private static void OnUnhandledException(object? _, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception exception)
			Log.Fatal(exception, "Unhandled exception");
		else
			Log.Fatal("Unhandled exception: {ExceptionObject}", e.ExceptionObject);

		Log.CloseAndFlush();
	}

	private static void OnUnobservedTaskException(object? _, UnobservedTaskExceptionEventArgs e)
	{
		Log.Error(e.Exception, "Unobserved task exception");
		e.SetObserved();
	}
}
