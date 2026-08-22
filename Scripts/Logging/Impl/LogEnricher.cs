using Godot;
using Serilog.Core;
using Serilog.Events;
using Environment = System.Environment;

namespace Root.Scripts.Logging.Impl;

public sealed class LogEnricher : ILogEventEnricher
{
	// Add some random enrichment data to the logs. I believe there's a NuGet package for this, but whatever.
	public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	{
		logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
			"ThreadId",
			Environment.CurrentManagedThreadId));

		logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
			"Frame",
			Engine.GetProcessFrames()));
	}
}
