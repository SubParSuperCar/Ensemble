using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Common.Text;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Scripts.Chat;
using EnsembleRoot.Scripts.Profanity;
using EnsembleRoot.Sessions.Actions;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Services;
using Estragonia;
using Godot;
using Color = Avalonia.Media.Color;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class TextChatViewModel : ViewModelBase, IWidget
{
	private static readonly StringName FocusAction = "ui_chat_focus";

	private readonly DispatcherService _dispatcher;

	public TextChatViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;

		foreach (var message in GChatManager.Messages)
			OnMessageAdded(message);
		GChatManager.MessageAdded += OnMessageAdded;

		GProfanityFilterManager.EnabledChanged += OnProfanityFilterEnabledChanged;
		GSessionManager.ActionRejected += OnActionRejected;
		dispatcher.Input += OnInput;
	}

	public ObservableCollection<TextChatLine> Lines { get; } = [];

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SendCommand))]
	public partial string Message { get; set; } = string.Empty;

	public bool IsHost { get; } = GSessionManager.IsServer;

	[ObservableProperty] public partial bool IsProfanityFilterEnabled { get; set; } = GProfanityFilterManager.IsEnabled;
	[ObservableProperty] public partial bool IsFileLoggingEnabled { get; set; } = GChatManager.IsFileLoggingEnabled;
	[ObservableProperty] public partial bool IsTtsEnabled { get; set; } = GChatManager.IsTtsEnabled;

	public static WidgetDescriptor Descriptor { get; } = new(WidgetDescriptor.Cells(26, 21, 6, 9))
	{
		Description = "Sends and shows this session's text chat messages.",
		MinSize = new Size(320, 256)
	};

	/// <summary>Raised when the player presses the focus key (/) while not typing elsewhere.</summary>
	public event Action? FocusRequested;

	protected override void OnDispose()
	{
		GChatManager.MessageAdded -= OnMessageAdded;

		GProfanityFilterManager.EnabledChanged -= OnProfanityFilterEnabledChanged;
		GSessionManager.ActionRejected -= OnActionRejected;
		_dispatcher.Input -= OnInput;
	}

	[RelayCommand(CanExecute = nameof(CanSend))]
	private void Send()
	{
		SendChatMessageAction.Send(Message);
		Message = string.Empty;
	}

	private bool CanSend() => !string.IsNullOrWhiteSpace(Message);

	// Keeps pasted line breaks from growing the single-line input
	partial void OnMessageChanged(string value)
	{
		if (value.AsSpan().ContainsAny('\r', '\n'))
			Message = value.ReplaceLineEndings(" ");
	}

	partial void OnIsProfanityFilterEnabledChanged(bool value)
	{
		if (value == GProfanityFilterManager.IsEnabled)
			return;

		ProfanityFilterManager.IsPreferred = value;
		new SetProfanityFilterAction(value).Submit();
	}

	partial void OnIsFileLoggingEnabledChanged(bool value) => GChatManager.IsFileLoggingEnabled = value;

	partial void OnIsTtsEnabledChanged(bool value) => GChatManager.IsTtsEnabled = value;

	private void OnMessageAdded(ChatMessage message)
	{
		if (message.IsNotice)
		{
			AddLine(new TextChatLine(null, default, message.Text));
			return;
		}

		var nametag = $"[{ChatText.ShortenName(message.SenderName)}]:";
		AddLine(new TextChatLine(nametag, message.SenderColor.ToAvaloniaColor(), message.Text));
	}

	// Local only, so the sender learns why a message went nowhere
	private void OnActionRejected(string actionId, string reason)
	{
		if (actionId is nameof(SendChatMessageAction) or nameof(SetProfanityFilterAction))
			AddLine(new TextChatLine(null, default, reason));
	}

	private void OnProfanityFilterEnabledChanged(bool isEnabled) => IsProfanityFilterEnabled = isEnabled;

	private void OnInput(InputEvent @event)
	{
		if (!InputSink.IsSunk && Input.IsActionJustPressedByEvent(FocusAction, @event))
			FocusRequested?.Invoke();
	}

	private void AddLine(TextChatLine line)
	{
		if (Lines.Count >= ChatManager.MaxHistoryCount)
			Lines.RemoveAt(0);

		Lines.Add(line);
	}
}

/// <summary>A chat log line: a player's colored nametag and message, or a notice without a nametag.</summary>
public sealed record TextChatLine(string? Nametag, Color NametagColor, string Text)
{
	public bool IsNotice => Nametag is null;
}
