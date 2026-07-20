using System.Globalization;
using Godot;
using Serilog.Core;
using Serilog.Events;
using Environment = System.Environment;

namespace Root.Scripts.Logger.Impl;

public sealed class LogSink : ILogEventSink
{
	public void Emit(LogEvent logEvent)
	{
		var message = string.Format(
			CultureInfo.InvariantCulture,
			"[{0:HH:mm:ss.fff}] [{1:u3}] {2}",
			logEvent.Timestamp,
			logEvent.Level,
			logEvent.RenderMessage(CultureInfo.InvariantCulture));

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
