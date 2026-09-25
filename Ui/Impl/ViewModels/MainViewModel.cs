using Avalonia.Controls;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Attributes;
using EnsembleRoot.Ui.Impl.Messages;
using EnsembleRoot.Ui.Impl.Services;
using Godot;
using Iciclecreek.Terminal;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using XTerm.Common;
using Color = Avalonia.Media.Color;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class MainViewModel : ViewModelBase
{
	private readonly DispatcherService _dispatcher;
	private readonly IServiceProvider _services;
	private readonly List<TerminalWindow> _terminals = [];

	public MainViewModel(IServiceProvider services, DispatcherService dispatcher)
	{
		_services = services;
		_dispatcher = dispatcher;

		Stats = services.GetRequiredService<StatViewModel>();

		dispatcher.Input += OnInput;
		dispatcher.Notification += OnNotification;

		if (GSessionManager.IsActive)
			OnSessionStarted();
		else
			OnSessionStopped();

		GSessionManager.SessionStarted += OnSessionStarted;
		GSessionManager.SessionStopped += OnSessionStopped;
	}

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial ViewModelBase? Main { get; set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial StatViewModel? Stats { get; set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial ConsoleViewModel? Console { get; set; }

	[ObservableProperty] public partial bool IsConsoleVisible { get; set; }

	protected override void OnDispose()
	{
		GSessionManager.SessionStarted -= OnSessionStarted;
		GSessionManager.SessionStopped -= OnSessionStopped;

		_dispatcher.UiProcess -= OnUiProcess;
		_dispatcher.Input -= OnInput;
		_dispatcher.Notification -= OnNotification;

		Main = null;
		Stats = null;
		IsConsoleVisible = false;
	}

	[RelayCommand]
	private void OpenTerminal() => ShowNewTerminalWindow();

	private static void OnUiProcess(UiProcessData data) => RenderingServer.ForceDraw();

	private void OnSessionStarted()
	{
		_dispatcher.UiProcess -= OnUiProcess;

		Log.Debug("Stopped forced render drawing");

		Main = _services.GetRequiredService<GameViewModel>();
	}

	private void OnSessionStopped()
	{
		_dispatcher.UiProcess += OnUiProcess;

		Log.Debug("Started forced render drawing...");

		Main = _services.GetRequiredService<MenuViewModel>();
	}

	private void OnInput(InputEvent @event)
	{
		if (Input.IsActionJustPressedByEvent("ui_toggle_console", @event))
			IsConsoleVisible = !IsConsoleVisible;
		else if (Input.IsActionJustPressedByEvent("ui_open_terminal", @event))
			ShowNewTerminalWindow();
	}

	private void OnNotification(int what)
	{
		if (what != Node.NotificationWMCloseRequest)
			return;

		foreach (var window in _terminals.ToArray())
			window.Close();
	}

	partial void OnIsConsoleVisibleChanging(bool value)
	{
		if (value)
		{
			Console = _services.GetRequiredService<ConsoleViewModel>();
			Log.Debug("Opened {Control}", nameof(ConsoleViewModel));
		}
		else
		{
			Console = null;
			Log.Debug("Closed {Control}", nameof(ConsoleViewModel));
		}
	}

	private void ShowNewTerminalWindow()
	{
		var terminal = new TerminalWindow
		{
			Width = 1280,
			Height = 720,
			CursorStyle = CursorStyle.Block,
			CursorColor = Color.Parse("#40A0FF"),
			CursorBlink = true,
			CursorBlinkRate = (int)TimeSpan.MillisecondsPerSecond / 3
		};

		_terminals.Add(terminal);

		terminal.Closing += OnClosing;
		terminal.ProcessExited += OnProcessExited;

		Log.Debug("PTY process created with shell: {Shell}", terminal.Process);

		terminal.Show();

		var editor = terminal
			.GetVisualDescendants()
			.OfType<TerminalView>()
			.FirstOrDefault();

		editor?.Focus();
		return;

		void OnClosing(object? sender, WindowClosingEventArgs e)
		{
			_terminals.Remove(terminal);
		}
	}

	private static void OnProcessExited(object? sender, ProcessExitedEventArgs e) =>
		Log.Debug("PTY process exited with code: {ExitCode}", e.ExitCode);
}
