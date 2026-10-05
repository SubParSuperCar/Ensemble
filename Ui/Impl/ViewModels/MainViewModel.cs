using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Attributes;
using EnsembleRoot.Ui.Impl.Extensions;
using EnsembleRoot.Ui.Impl.Messages;
using EnsembleRoot.Ui.Impl.Services;
using Godot;
using Iciclecreek.Terminal;
using Serilog;
using XTerm.Common;
using Color = Avalonia.Media.Color;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class MainViewModel : ViewModelBase
{
	private const int TargetTerminalFps = 60;

	private readonly DispatcherService _dispatcher;
	private readonly IServiceProvider _services;
	private readonly List<TerminalWindow> _terminals = [];

	static MainViewModel()
	{
		TerminalRenderThrottle.TargetFrameRate = TargetTerminalFps;
	}

	public MainViewModel(IServiceProvider services, DispatcherService dispatcher)
	{
		_services = services;
		_dispatcher = dispatcher;

		Stats = services.Create<StatViewModel>();

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

	// Force Godot to keep rendering to keep the UI going,
	// even though it normally wouldn't because there's no 3D scene when out of session
	private static void OnUiProcess(UiProcessData data) => RenderingServer.ForceDraw();

	private void OnSessionStarted()
	{
		_dispatcher.UiProcess -= OnUiProcess;

		Log.Debug("Stopped forced render drawing");

		Main = _services.Create<GameViewModel>();
	}

	private void OnSessionStopped()
	{
		_dispatcher.UiProcess += OnUiProcess;

		Log.Debug("Started forced render drawing");

		Main = _services.Create<MenuViewModel>();
	}

	private void OnInput(InputEvent @event)
	{
		if (Input.IsActionJustPressedByEvent("ui_toggle_console", @event))
			IsConsoleVisible = !IsConsoleVisible;
		else if (Input.IsActionJustPressedByEvent("ui_open_pty", @event))
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
			Console = _services.Create<ConsoleViewModel>();
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
		var app = Application.Current!;
		var isDark = app.ActualThemeVariant == ThemeVariant.Dark;

		// Sync the appearance to the main UI
		var fontFamily = app.FindResource("Font") as FontFamily ?? FontFamily.Default;
		var fontSize = app.FindResource("FontSize") as double? ?? 16d;
		var cursorColor = (app.FindResource("HighlightBrush") as ISolidColorBrush)?.Color ?? Color.Parse("#40A0FF");
		var selectionBrush = app.FindResource("ThemeAccentBrush3") as IBrush ?? new SolidColorBrush(cursorColor, 0.4);

		// TODO: Consider adding support for translucent terminal windows. Estragonia is likely the limiting factor.
		var terminal = new TerminalWindow
		{
			Width = 1280,
			Height = 720,
			FontFamily = fontFamily,
			FontSize = fontSize,
			Foreground = isDark ? Brushes.White : Brushes.Black,
			Background = isDark ? Brushes.Black : Brushes.White,
			SelectionBrush = selectionBrush,
			Ligatures = true,
			CursorStyle = CursorStyle.Block,
			CursorColor = cursorColor,
			CursorBlink = true,
			CursorBlinkRate = (int)TimeSpan.MillisecondsPerSecond / 3
		};

		_terminals.Add(terminal);

		terminal.Closing += OnClosing;
		terminal.ProcessExited += OnProcessExited;

		Log.Debug("PTY process created with shell: {Shell}", terminal.Process);

		terminal.Show();
		terminal.GetVisualDescendants().OfType<TerminalView>().FirstOrDefault()?.Focus();

		return;

		void OnClosing(object? sender, WindowClosingEventArgs e)
		{
			_terminals.Remove(terminal);
		}
	}

	private static void OnProcessExited(object? sender, ProcessExitedEventArgs e) =>
		Log.Debug("PTY process exited with code: {ExitCode}", e.ExitCode);
}
