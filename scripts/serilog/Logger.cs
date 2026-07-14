using System.Globalization;
using Godot;
using Root.Scripts.StdOut.Impl;
using Serilog;

namespace Root.Scripts.StdOut;

public partial class Logger : Node
{
	public override void _EnterTree()
	{
		var logDirectory = ProjectSettings.GlobalizePath("user://logs");
		Directory.CreateDirectory(logDirectory);

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
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
	}

	public override void _ExitTree() => Log.CloseAndFlush();
}
