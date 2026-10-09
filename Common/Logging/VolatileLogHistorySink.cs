using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;

namespace EnsembleRoot.Common.Logging;

/// <summary>Keeps the most recent log entries in memory for the in-game log output and toasts.</summary>
/// <remarks>Verbose entries have their own capacity, so verbose spam can't evict the more important entries.</remarks>
public sealed class VolatileLogHistorySink : ILogEventSink
{
	private const int MaxEntries = 512;
	private const int MaxVerboseEntries = 256;

	private static readonly Lock HistoryLock = new();
	private static readonly Queue<(long Sequence, LogEntry Entry)> Entries = [];
	private static readonly Queue<(long Sequence, LogEntry Entry)> VerboseEntries = [];

	private static readonly ExpressionTemplate MessageFormatter = new("{@m:lj}");

	private static readonly ExpressionTemplate TextFormatter = new(
		"[{@t:HH:mm:ss.fff} {@l:u3}] {@m:lj}{#if @x is not null}\n{@x}{#end}");

	private static long _sequence;

	/// <summary>Gets a snapshot of the recorded entries, oldest first.</summary>
	public static IReadOnlyList<LogEntry> History
	{
		get
		{
			lock (HistoryLock)
				return Merge(Entries, VerboseEntries);
		}
	}

	public void Emit(LogEvent logEvent)
	{
		var entry = new LogEntry(
			logEvent.Timestamp, logEvent.Level, Format(MessageFormatter, logEvent), Format(TextFormatter, logEvent));

		var isVerbose = logEvent.Level is LogEventLevel.Verbose;
		var (queue, capacity) = isVerbose ? (VerboseEntries, MaxVerboseEntries) : (Entries, MaxEntries);

		lock (HistoryLock)
		{
			if (queue.Count >= capacity)
				queue.Dequeue();

			queue.Enqueue((++_sequence, entry));
		}

		Emitted?.Invoke(entry);
		Updated?.Invoke();
	}

	/// <summary>Raised on the logging thread for every recorded entry.</summary>
	public static event Action<LogEntry>? Emitted;

	/// <summary>Raised on the changing thread whenever <see cref="History" /> changes (including clears).</summary>
	public static event Action? Updated;

	public static void Clear()
	{
		lock (HistoryLock)
		{
			Entries.Clear();
			VerboseEntries.Clear();
		}

		Updated?.Invoke();
	}

	private static LogEntry[] Merge(
		Queue<(long Sequence, LogEntry Entry)> left, Queue<(long Sequence, LogEntry Entry)> right)
	{
		var merged = new LogEntry[left.Count + right.Count];
		using var leftItems = left.GetEnumerator();
		using var rightItems = right.GetEnumerator();

		var hasLeft = leftItems.MoveNext();
		var hasRight = rightItems.MoveNext();

		for (var i = 0; i < merged.Length; i++)
		{
			if (hasLeft && (!hasRight || leftItems.Current.Sequence < rightItems.Current.Sequence))
			{
				merged[i] = leftItems.Current.Entry;
				hasLeft = leftItems.MoveNext();
			}
			else
			{
				merged[i] = rightItems.Current.Entry;
				hasRight = rightItems.MoveNext();
			}
		}

		return merged;
	}

	private static string Format(ExpressionTemplate formatter, LogEvent logEvent)
	{
		using var writer = new StringWriter();
		formatter.Format(logEvent, writer);

		return writer.ToString().TrimEnd();
	}
}
