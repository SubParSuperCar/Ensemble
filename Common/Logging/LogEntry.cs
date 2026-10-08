using Serilog.Events;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace EnsembleRoot.Common.Logging;

/// <param name="Message">The rendered message alone.</param>
/// <param name="Text">The full formatted line, with its timestamp, level, and any exception.</param>
public sealed record LogEntry(DateTimeOffset Timestamp, LogEventLevel Level, string Message, string Text);
