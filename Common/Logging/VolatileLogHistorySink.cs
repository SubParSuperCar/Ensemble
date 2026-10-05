using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;

namespace EnsembleRoot.Common.Logging;

public sealed class VolatileLogHistorySink : ILogEventSink
{
	private const ushort MaxEntries = 500;

	private static readonly ConcurrentQueue<LogEntry> HistoryQueue = [];

	private static readonly ExpressionTemplate MessageFormatter = new("{@m:lj}");

	private static readonly ExpressionTemplate TextFormatter = new(
		"[{@t:HH:mm:ss.fff} {@l:u3}] {@m:lj}{#if @x is not null}\n{@x}{#end}");

	public static IReadOnlyCollection<LogEntry> History => [.. HistoryQueue];

	public void Emit(LogEvent logEvent)
	{
		if (logEvent.Level is LogEventLevel.Verbose)
			return;

		var entry = new LogEntry(
			logEvent.Timestamp,
			logEvent.Level,
			Format(MessageFormatter, logEvent),
			Format(TextFormatter, logEvent));

		while (HistoryQueue.Count >= MaxEntries && HistoryQueue.TryDequeue(out _)) { }

		HistoryQueue.Enqueue(entry);
		Emitted?.Invoke(entry);
		Updated?.Invoke();
	}

	/// <summary>Raised on the logging thread for every recorded entry.</summary>
	public static event Action<LogEntry>? Emitted;

	public static event Action? Updated;

	public static void Clear()
	{
		HistoryQueue.Clear();
		Updated?.Invoke();
	}

	private static string Format(ExpressionTemplate formatter, LogEvent logEvent)
	{
		using var writer = new StringWriter();
		formatter.Format(logEvent, writer);

		return writer.ToString().TrimEnd();
	}
}
