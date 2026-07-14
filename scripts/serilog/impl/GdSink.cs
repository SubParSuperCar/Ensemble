using System.Globalization;
using Godot;
using Serilog.Core;
using Serilog.Events;
using Environment = System.Environment;

namespace Root.Scripts.StdOut.Impl;

public sealed class GdSink : ILogEventSink
{
	public void Emit(LogEvent logEvent)
	{
		var output = string.Format(
			CultureInfo.InvariantCulture,
			"[{0:HH:mm:ss.fff}] [{1:u3}] {2}",
			logEvent.Timestamp,
			logEvent.Level,
			logEvent.RenderMessage(CultureInfo.InvariantCulture));

		if (logEvent.Exception is not null)
			output = string.Concat(output, Environment.NewLine, logEvent.Exception);

		switch (logEvent.Level)
		{
			case LogEventLevel.Error or LogEventLevel.Fatal:
				GD.PushError(output);
				break;
			case LogEventLevel.Warning:
				GD.PushWarning(output);
				break;
			default:
				GD.Print(output);
				break;
		}
	}
}
