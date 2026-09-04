using System.Globalization;
using Godot;
using Serilog.Core;
using Serilog.Events;
using Environment = System.Environment;

namespace Root.Scripts.Logging.Impl;

public sealed class LogSink : ILogEventSink
{
	public void Emit(LogEvent logEvent)
	{
		var renderedMessage = logEvent.RenderMessage(CultureInfo.InvariantCulture);

		var level = logEvent.Level switch
		{
			LogEventLevel.Verbose => "VRB",
			LogEventLevel.Debug => "DBG",
			LogEventLevel.Information => "INF",
			LogEventLevel.Warning => "WRN",
			LogEventLevel.Error => "ERR",
			LogEventLevel.Fatal => "FTL",
			_ => "???"
		};

		var message = string.Create(
			CultureInfo.InvariantCulture,
			$"[{logEvent.Timestamp:HH:mm:ss.fff}] [{level}] {renderedMessage}");

		if (logEvent.Exception is not null)
			message = string.Concat(message, Environment.NewLine, logEvent.Exception);

		switch (logEvent.Level)
		{
			case LogEventLevel.Error or LogEventLevel.Fatal:
				GD.PushError(message);
				break;

			case LogEventLevel.Warning:
				GD.PushWarning(message);
				break;

			default:
				GD.Print(message);
				break;
		}
	}
}
