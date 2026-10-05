using System.Globalization;
using EnsembleRoot.Common.Logging;
using Godot;
using Serilog.Core;
using Serilog.Events;

namespace EnsembleRoot.Scripts.Logging.Impl;

public sealed class LogSink : ILogEventSink
{
	public void Emit(LogEvent logEvent)
	{
		var renderedMessage = logEvent.RenderMessage(CultureInfo.InvariantCulture);

		var message = string.Create(
			CultureInfo.InvariantCulture,
			$"[{logEvent.Timestamp:HH:mm:ss.fff}] [{logEvent.Level.Abbreviation}] {renderedMessage}");

		if (logEvent.Exception is not null)
			message = string.Concat(message, "\n", logEvent.Exception);

		if (logEvent.Properties.TryGetValue(NativeFrames.PropertyName, out var nativeFrames))
			message = string.Concat(message, "\nNative frames:\n", (nativeFrames as ScalarValue)?.Value);

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
