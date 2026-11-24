using EnsembleRoot.Common.Text;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

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

	// Themed as the console is, wherever the output goes, for the in-game log to color alike. A literal or JSON
	// message (lj) would take one color, where a plain one colors each value.
	private static readonly ExpressionTemplate TextFormatter = new(
		"[{@t:HH:mm:ss.fff} {@l:u3}] {@m}{#if @x is not null}\n{@x}{#end}", theme: TemplateTheme.Code,
		applyThemeWhenOutputIsRedirected: true);

	private static long _sequence;

	public void Emit(LogEvent logEvent)
	{
		var (text, styles) = AnsiText.Parse(Format(TextFormatter, logEvent));
		var entry = new LogEntry(logEvent.Level, Format(MessageFormatter, logEvent), text, styles);

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

	/// <summary>Raised on the changing thread whenever the history changes (including clears).</summary>
	public static event Action? Updated;

	/// <summary>Replaces a list's contents with the recorded entries at or above a level, oldest first.</summary>
	public static void CopyHistory(ICollection<LogEntry> destination, LogEventLevel minimumLevel)
	{
		destination.Clear();

		lock (HistoryLock)
			Merge(Entries, VerboseEntries, destination, minimumLevel);
	}

	public static void Clear()
	{
		lock (HistoryLock)
		{
			Entries.Clear();
			VerboseEntries.Clear();
		}

		Updated?.Invoke();
	}

	private static void Merge(
		Queue<(long Sequence, LogEntry Entry)> left, Queue<(long Sequence, LogEntry Entry)> right,
		ICollection<LogEntry> destination, LogEventLevel minimumLevel)
	{
		using var leftItems = left.GetEnumerator();
		using var rightItems = right.GetEnumerator();

		var hasLeft = leftItems.MoveNext();
		var hasRight = rightItems.MoveNext();

		while (hasLeft || hasRight)
		{
			LogEntry entry;

			if (hasLeft && (!hasRight || leftItems.Current.Sequence < rightItems.Current.Sequence))
			{
				entry = leftItems.Current.Entry;
				hasLeft = leftItems.MoveNext();
			}
			else
			{
				entry = rightItems.Current.Entry;
				hasRight = rightItems.MoveNext();
			}

			if (entry.Level >= minimumLevel)
				destination.Add(entry);
		}
	}

	private static string Format(ExpressionTemplate formatter, LogEvent logEvent)
	{
		using var writer = new StringWriter();
		formatter.Format(logEvent, writer);

		return writer.ToString().TrimEnd();
	}
}
