using System.Globalization;
using Godot;
using Godot.Collections;
using Serilog;
using Serilog.Events;

namespace EnsembleRoot.Scripts.Logging;

/// <summary>
///     Forwards the engine's own output (Godot's errors and warnings, its addons', and prints) to the game's logs, so
///     the log files and the in-game log output carry them too.
/// </summary>
/// <remarks>
///     The engine writes them to the terminal and the editor itself, so the sinks writing there skip them
///     (<see cref="IsFromEngine" />), and what <see cref="LogSink" /> prints through the engine isn't forwarded back.
///     Prints are verbose, as the editor's verbose output is hundreds of lines. The engine calls in from any thread.
/// </remarks>
internal sealed partial class EngineLog : Godot.Logger
{
	private const string SourceProperty = "Source";
	private const string SourceName = "Godot";

	private readonly ILogger _log = Log.ForContext(SourceProperty, SourceName);

	public static bool IsFromEngine(LogEvent logEvent) =>
		logEvent.Properties.TryGetValue(SourceProperty, out var source) && source is ScalarValue { Value: SourceName };

	public override void _LogMessage(string message, bool error)
	{
		if (!LogSink.IsEmitting)
			Write(error ? LogEventLevel.Warning : LogEventLevel.Verbose, message.TrimEnd());
	}

	public override void _LogError(
		string function, string file, int line, string code, string rationale, bool editorNotify, int errorType,
		Array<ScriptBacktrace> scriptBacktraces)
	{
		if (LogSink.IsEmitting)
			return;

		var level = (ErrorType)errorType is ErrorType.Warning ? LogEventLevel.Warning : LogEventLevel.Error;

		var message = string.IsNullOrEmpty(rationale) ? code : rationale;
		Write(level, string.Create(CultureInfo.InvariantCulture, $"{message} (at {function} in {file}:{line})"));
	}

	// As the template itself, escaped, so the log view shows it as plain text rather than a value it colors
	// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
#pragma warning disable CA2254
	private void Write(LogEventLevel level, string text) =>
		_log.Write(
			level, text.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal));
#pragma warning restore CA2254
}
