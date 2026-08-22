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
		// Pull this variable out so the next one isn't so long.
		var renderedMessage = logEvent.RenderMessage(CultureInfo.InvariantCulture);

		var message = string.Create(
			CultureInfo.InvariantCulture,
			$"[{logEvent.Timestamp:HH:mm:ss.fff}] [{logEvent.Level:u3}] {renderedMessage}");

		if (logEvent.Exception is not null)
			message = string.Concat(message, Environment.NewLine, logEvent.Exception);

		// Push the logs to Godot too so they are visible in Godot's output window.
		switch (logEvent.Level)
		{
			case LogEventLevel.Error or LogEventLevel.Fatal: // Error/Fatal both go to PushError.
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
