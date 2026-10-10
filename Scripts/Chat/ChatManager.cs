using System.Globalization;
using System.Text;
using EnsembleRoot.Autoloading;
using EnsembleRoot.Common.Utils;
using EnsembleRoot.GdCore.Players;
using Godot;
using Godot.Collections;
using Serilog;

namespace EnsembleRoot.Scripts.Chat;

/// <summary>
///     Keeps the current session's chat: its latest <see cref="MaxHistoryCount" /> messages, oldest first, and whether
///     the host filters profanity. Optionally appends messages to a plain-text file per session in
///     <see cref="ChatLogDir" />, up to <see cref="MaxLogFileBytes" />.
/// </summary>
/// <remarks>
///     Messages and filter changes arrive as network actions, and late joiners receive the history as a snapshot (see
///     Replication). When hosting, the filter starts as <see cref="FilterOverride" />, if set, or
///     <see cref="IsFilterPreferred" />.
/// </remarks>
[GlobalClass]
[Autoload(Order = AutoloadOrder.Standard, FailurePolicy = AutoloadFailurePolicy.LogAndContinue)]
public partial class ChatManager : Node, IAutoload
{
	[Signal]
	public delegate void FilterEnabledChangedEventHandler(bool isEnabled);

	[Signal]
	public delegate void MessageAddedEventHandler(ChatMessage message);

	public const int MaxHistoryCount = 128;
	public const long MaxLogFileBytes = 4 * 1024 * 1024;

	private const string Section = "chat";
	private const string IsFilterPreferredKey = "filter_enabled";
	private const string IsFileLoggingEnabledKey = "log_to_file";

	private readonly Queue<ChatMessage> _messages = [];
	private bool _isFileLoggingEnabled;
	private bool _isLogStopped;
	private StreamWriter? _log;

	public static ChatManager? Instance { get; private set; }

	public IReadOnlyCollection<ChatMessage> Messages => _messages;
	public bool IsFilterEnabled { get; private set; }

	/// <summary>Whether to filter profanity when hosting. Persisted.</summary>
	public static bool IsFilterPreferred
	{
		get => UserData.GetValue(Section, IsFilterPreferredKey, true).AsBool();
		set => UserData.SetValue(Section, IsFilterPreferredKey, value);
	}

	/// <summary>Overrides <see cref="IsFilterPreferred" /> without persisting it (e.g., from user args).</summary>
	public static bool? FilterOverride { get; set; }

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

	public void Initialize()
	{
		Instance = this;
		_isFileLoggingEnabled = UserData.GetValue(Section, IsFileLoggingEnabledKey, false).AsBool();

		GSessionManager.SessionStarted += OnSessionStarted;
		GSessionManager.SessionStopped += OnSessionStopped;
		GPlayers.Added += OnPlayerAdded;
		GPlayers.Removed += OnPlayerRemoved;
	}

	public override void _ExitTree()
	{
		GSessionManager.SessionStarted -= OnSessionStarted;
		GSessionManager.SessionStopped -= OnSessionStopped;
		GPlayers.Added -= OnPlayerAdded;
		GPlayers.Removed -= OnPlayerRemoved;

		CloseLog();

		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public Array<ChatMessage> GetAllMessages() => [.. _messages];

	internal void Add(ChatMessage message)
	{
		if (_messages.Count >= MaxHistoryCount)
			_messages.Dequeue();

		_messages.Enqueue(message);
		WriteToLog(message);

		EmitSignal(SignalName.MessageAdded, message);
	}

	internal void SetFilterEnabled(bool isEnabled, bool announce)
	{
		if (IsFilterEnabled == isEnabled)
			return;

		IsFilterEnabled = isEnabled;
		EmitSignal(SignalName.FilterEnabledChanged, isEnabled);

		if (announce)
			Add(new ChatMessage { Text = $"The host turned the profanity filter {(isEnabled ? "on" : "off")}." });
	}

	private void OnSessionStarted()
	{
		if (GSessionManager.IsServer)
			SetFilterEnabled(FilterOverride ?? IsFilterPreferred, false);
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
		SetFilterEnabled(false, false);

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

		var path = Path.Join(dir, fileName);
		Log.Information("Logging chat to {Path}", path);

		return new StreamWriter(path, true, new UTF8Encoding(false)) { AutoFlush = true };
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
}
