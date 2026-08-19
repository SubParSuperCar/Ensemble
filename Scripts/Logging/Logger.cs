using System.Globalization;
using Godot;
using Microsoft.Extensions.Logging;
using Root.Autoloading;
using Root.Scripts.Logging.Impl;
using Serilog;

namespace Root.Scripts.Logging;

[GlobalClass]
[Autoload(Order = sbyte.MinValue)]
public partial class Logger : Node, IAutoload
{
	private ILoggerFactory? _loggerFactory;

	public void Initialize()
	{
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.Enrich.With(new LogEnricher())
			.WriteTo.Sink(new LogSink())
			.WriteTo.Console(
				outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
				formatProvider: CultureInfo.InvariantCulture)
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
