using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EnsembleRoot.Autoloading;
using EnsembleRoot.Common.Audio;
using EnsembleRoot.Common.Text;
using EnsembleRoot.Common.Utils;
using EnsembleRoot.GdCore.Players;
using Godot;
using Godot.Collections;
using Serilog;

namespace EnsembleRoot.Scripts.Chat;

/// <summary>
///     Keeps the current session's chat: its latest <see cref="MaxHistoryCount" /> messages, oldest first. Optionally
///     appends messages to a plain-text file per session in <see cref="ChatLogDir" />, up to
///     <see cref="MaxLogFileBytes" />, and reads new messages aloud.
/// </summary>
/// <remarks>
///     Messages arrive as network actions, and late joiners receive the history as a snapshot (see Replication). The
///     host's profanity filter masks them first (see <see cref="Profanity.ProfanityFilterManager" />).
/// </remarks>
[GlobalClass]
[Autoload(Order = AutoloadOrder.Standard, FailurePolicy = AutoloadFailurePolicy.LogAndContinue)]
public partial class ChatManager : Node, IAutoload
{
	[Signal]
	public delegate void MessageAddedEventHandler(ChatMessage message);

	public const int MaxHistoryCount = 128;
	public const long MaxLogFileBytes = 4 * 1024 * 1024;

	// Like the Serilog file sink's retainedFileCountLimit
	private const int MaxLogFileCount = 30;
	private const string LogFilePattern = "chat_*.log";

	private const string Section = "chat";
	private const string IsFileLoggingEnabledKey = "log_to_file";
	private const string IsTtsEnabledKey = "tts_enabled";

	private readonly Queue<ChatMessage> _messages = [];
	private bool _isFileLoggingEnabled;
	private bool _isLogStopped;
	private bool _isTtsEnabled;
	private StreamWriter? _log;

	// Never disposed: waiting utterances still read its token on the speech thread, and it has no timer to release
	private CancellationTokenSource _speechCts = new();

	[GeneratedRegex("#{2,}", RegexOptions.None, RegexMatchTimeoutMs)]
	private static partial Regex MaskRunRegex { get; }

	public static ChatManager? Instance { get; private set; }

	public IReadOnlyCollection<ChatMessage> Messages => _messages;

	/// <summary>Whether to append messages to a file per session in <see cref="ChatLogDir" />. Persisted.</summary>
	public bool IsFileLoggingEnabled
	{
		get => _isFileLoggingEnabled;
		set
		{
			_isFileLoggingEnabled = value;
			UserData.SetValue(Section, IsFileLoggingEnabledKey, value);

			if (!value)
				CloseLog();
		}
	}

	/// <summary>Whether to read new messages aloud. Persisted.</summary>
	public bool IsTtsEnabled
	{
		get => _isTtsEnabled;
		set
		{
			_isTtsEnabled = value;
			UserData.SetValue(Section, IsTtsEnabledKey, value);

			if (!value)
				StopSpeech();
		}
	}

	public void Initialize()
	{
		Instance = this;

		_isFileLoggingEnabled = UserData.GetValue(Section, IsFileLoggingEnabledKey, false).AsBool();

		// Dedicated hosts share the user data but have no listener
		_isTtsEnabled = !Main.IsHeadlessServer && UserData.GetValue(Section, IsTtsEnabledKey, false).AsBool();

		GSessionManager.SessionStopped += OnSessionStopped;

		GPlayers.Added += OnPlayerAdded;
		GPlayers.Removed += OnPlayerRemoved;
	}

	public override void _ExitTree()
	{
		GSessionManager.SessionStopped -= OnSessionStopped;

		GPlayers.Added -= OnPlayerAdded;
		GPlayers.Removed -= OnPlayerRemoved;

		CloseLog();
		StopSpeech();

		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public Array<ChatMessage> GetAllMessages() => [.. _messages];

	/// <summary>Adds a message, reading it aloud unless it predates joining (<paramref name="isHistory" />).</summary>
	internal void Add(ChatMessage message, bool isHistory = false)
	{
		if (_messages.Count >= MaxHistoryCount)
			_messages.Dequeue();

		_messages.Enqueue(message);
		WriteToLog(message);

		if (IsTtsEnabled && !isHistory)
			TextToSpeech.Speak(ToSpeech(message), token: _speechCts.Token);

		EmitSignal(SignalName.MessageAdded, message);
	}

	// Only while active, so neither players registered while joining (the roster and the local player) nor the
	// session's teardown read as players coming and going. Joiners get their own notice from the host's history.
	private void OnPlayerAdded(GdPlayer player)
	{
		if (GSessionManager.IsActive)
			Add(new ChatMessage { Text = $"{player.Name} joined the session." });
	}

	private void OnPlayerRemoved(GdPlayer player)
	{
		if (GSessionManager.IsActive)
			Add(new ChatMessage { Text = $"{player.Name} left the session." });
	}

	private void OnSessionStopped()
	{
		_messages.Clear();
		StopSpeech();

		CloseLog();
		_isLogStopped = false;
	}

	private void WriteToLog(ChatMessage message)
	{
		if (!IsFileLoggingEnabled || _isLogStopped)
			return;

		try
		{
			_log ??= OpenLog();

			if (_log.BaseStream.Length >= MaxLogFileBytes)
			{
				_log.WriteLine("* The chat log reached its size limit, so logging stops for this session.");
				StopLog();

				return;
			}

			var postedAt = message.UtcPostedAt.ToLocalTime();
			_log.WriteLine(string.Create(CultureInfo.InvariantCulture, $"[{postedAt:yyyy-MM-dd HH:mm:ss}] {message}"));
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			Log.Warning(exception, "Failed to write the chat log; chat logging stops for this session");
			StopLog();
		}
	}

	private static StreamWriter OpenLog()
	{
		var dir = ProjectSettings.GlobalizePath(ChatLogDir);
		var fileName = string.Create(
			CultureInfo.InvariantCulture, $"chat_{GSessionManager.UtcStartedAt.ToLocalTime():yyyy-MM-dd_HH-mm-ss}.log");

		Directory.CreateDirectory(dir);
		PruneLogs(dir);

		var path = Path.Join(dir, fileName);
		Log.Information("Logging chat to {Path}", path);

		return new StreamWriter(path, true, new UTF8Encoding(false)) { AutoFlush = true };
	}

	// Keeps the newest logs, leaving room for the one about to open; the names sort by time
	private static void PruneLogs(string dir)
	{
		var stale = Directory.GetFiles(dir, LogFilePattern)
			.OrderDescending(StringComparer.Ordinal)
			.Skip(MaxLogFileCount - 1);

		foreach (var path in stale)
			try
			{
				File.Delete(path);
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
			{
				Log.Warning(exception, "Failed to delete old chat log {Path}", path);
			}
	}

	private void StopSpeech()
	{
		_speechCts.Cancel();
		_speechCts = new CancellationTokenSource();
	}

	private void StopLog()
	{
		_isLogStopped = true;
		CloseLog();
	}

	private void CloseLog()
	{
		_log?.Dispose();
		_log = null;
	}

	// Masks would be read aloud symbol by symbol, placeholders with their brackets, and an ellipsis by name
	private static string ToSpeech(ChatMessage message)
	{
		if (message.IsNotice)
			return message.Text;

		var text = message.Text is ProfanityFilter.Redacted or ProfanityFilter.Failed
			? message.Text[1..^1]
			: MaskRunRegex.Replace(message.Text, string.Empty);

		return $"{ChatText.ShortenName(message.SenderName, string.Empty)} said: {text}";
	}
}
