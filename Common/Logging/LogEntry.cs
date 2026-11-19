using EnsembleRoot.Common.Text;
using Serilog.Events;

namespace EnsembleRoot.Common.Logging;

/// <param name="Message">The rendered message alone.</param>
/// <param name="Text">The full formatted line, with its timestamp, level, and any exception.</param>
/// <param name="Styles">The runs of <paramref name="Text" /> the console's theme colors.</param>
public sealed record LogEntry(LogEventLevel Level, string Message, string Text, IReadOnlyList<AnsiRun> Styles);
