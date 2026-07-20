using Godot;
using Serilog.Core;
using Serilog.Events;
using Environment = System.Environment;

namespace Root.Scripts.Logger.Impl;

public sealed class LogEnricher : ILogEventEnricher
{
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
