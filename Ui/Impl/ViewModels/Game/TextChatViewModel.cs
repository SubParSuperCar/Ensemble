using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Scripts.Chat;
using EnsembleRoot.Sessions.Actions;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;
using Estragonia;
using Godot;
using Color = Avalonia.Media.Color;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed partial class TextChatViewModel : ViewModelBase, IWidget
{
	private static readonly StringName FocusAction = "ui_focus_chat";

	private readonly DispatcherService _dispatcher;

	public TextChatViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;

		foreach (var message in GChatManager.Messages)
			OnMessageAdded(message);

		GChatManager.MessageAdded += OnMessageAdded;
		GChatManager.FilterEnabledChanged += OnFilterEnabledChanged;
		GSessionManager.ActionRejected += OnActionRejected;
		dispatcher.Input += OnInput;
	}

	public ObservableCollection<TextChatLine> Lines { get; } = [];

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SendCommand))]
	public partial string Message { get; set; } = string.Empty;

	public bool IsHost { get; } = GSessionManager.IsServer;

	[ObservableProperty] public partial bool IsFilterEnabled { get; set; } = GChatManager.IsFilterEnabled;
	[ObservableProperty] public partial bool IsFileLoggingEnabled { get; set; } = GChatManager.IsFileLoggingEnabled;

	public static WidgetDescriptor Descriptor { get; } = new(WidgetDescriptor.Cells(12, 10, 4, 5))
	{
		Description = "Sends and shows this session's text chat messages.",
		MinSize = new Size(384, 256)
	};

	/// <summary>Raised when the player presses the focus key (/) while not typing elsewhere.</summary>
	public event Action? FocusRequested;

	protected override void OnDispose()
	{
		GChatManager.MessageAdded -= OnMessageAdded;
		GChatManager.FilterEnabledChanged -= OnFilterEnabledChanged;
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

	partial void OnIsFilterEnabledChanged(bool value)
	{
		if (value == GChatManager.IsFilterEnabled)
			return;

		ChatManager.IsFilterPreferred = value;
		new SetChatFilterAction(value).Submit();
	}

	partial void OnIsFileLoggingEnabledChanged(bool value) => GChatManager.IsFileLoggingEnabled = value;

	private void OnMessageAdded(ChatMessage message) =>
		AddLine(
			message.IsNotice
				? new TextChatLine(null, default, message.Text)
				: new TextChatLine($"[{message.SenderName}]:", message.SenderColor.ToAvaloniaColor(), message.Text));

	// Local only, so the sender learns why a message went nowhere
	private void OnActionRejected(string actionId, string reason)
	{
		if (actionId is nameof(SendChatMessageAction) or nameof(SetChatFilterAction))
			AddLine(new TextChatLine(null, default, reason));
	}

	private void OnFilterEnabledChanged(bool isEnabled) => IsFilterEnabled = isEnabled;

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
