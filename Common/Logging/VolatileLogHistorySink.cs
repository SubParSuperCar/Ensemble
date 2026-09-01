using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;

namespace Root.Common.Logging;

public sealed class VolatileLogHistorySink : ILogEventSink
{
	private const ushort MaxEntries = 500;

	private static readonly ConcurrentQueue<string> HistoryQueue = [];
	private static readonly ExpressionTemplate Formatter = new("[{@t:HH:mm:ss} {@l:u3}] {@m:lj}{@x}");

	public static IReadOnlyCollection<string> History => [.. HistoryQueue];

	public void Emit(LogEvent logEvent)
	{
		using var writer = new StringWriter();
		Formatter.Format(logEvent, writer);

		var entry = writer.ToString().TrimEnd();

		while (HistoryQueue.Count >= MaxEntries && HistoryQueue.TryDequeue(out _)) { }

		HistoryQueue.Enqueue(entry);
		Updated?.Invoke();
	}

	public static void Clear()
	{
		HistoryQueue.Clear();
		Updated?.Invoke();
	}

	public static event Action? Updated;
}
