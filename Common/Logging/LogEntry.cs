using Serilog.Events;

namespace EnsembleRoot.Common.Logging;

/// <param name="Message">The rendered message alone.</param>
/// <param name="Text">The full formatted line, with its timestamp, level, and any exception.</param>
// ReSharper disable once NotAccessedPositionalProperty.Global
public sealed record LogEntry(LogEventLevel Level, string Message, string Text);
