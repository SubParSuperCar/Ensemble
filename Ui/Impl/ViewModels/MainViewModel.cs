using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Attributes;
using EnsembleRoot.Ui.Extensions;
using EnsembleRoot.Ui.Services;
using Godot;
using Iciclecreek.Terminal;
using Serilog;
using XTerm.Common;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
	private const int TargetTerminalFps = 60;
	private const int TerminalWidth = 1280;
	private const int TerminalHeight = 720;
	private const int TerminalCursorBlinkRateMs = (int)TimeSpan.MillisecondsPerSecond / 3;

	private static readonly StringName ToggleConsoleAction = "ui_console_toggle";
	private static readonly StringName OpenTerminalAction = "ui_terminal_open";
	private static readonly StringName TogglePhotoModeAction = "ui_photo_mode_toggle";

	private static readonly TimeSpan PhotoModeHintDuration = TimeSpan.FromSeconds(3);

	private readonly DispatcherService _dispatcher;
	private readonly DispatcherTimer _photoModeTimer = new();
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
		Clock = services.Create<ClockViewModel>();
		Toasts = services.Create<ToastListViewModel>();

		dispatcher.Input += OnInput;
		dispatcher.Notification += OnNotification;
		_photoModeTimer.Tick += OnPhotoModeTimerTick;

		if (GSessionManager.IsActive)
			OnSessionStarted();
		else
			OnSessionStopped();

		GSessionManager.SessionStarted += OnSessionStarted;
		GSessionManager.SessionStopped += OnSessionStopped;
	}

	[ObservableProperty]
	[DisposeOldObservableValueOnChanging]
	public partial ViewModelBase? Main { get; private set; }

	[ObservableProperty]
	[DisposeOldObservableValueOnChanging]
	public partial StatViewModel? Stats { get; private set; }

	[ObservableProperty]
	[DisposeOldObservableValueOnChanging]
	public partial ClockViewModel? Clock { get; private set; }

	[ObservableProperty]
	[DisposeOldObservableValueOnChanging]
	public partial ToastListViewModel? Toasts { get; private set; }

	[ObservableProperty]
	[DisposeOldObservableValueOnChanging]
	public partial ConsoleViewModel? Console { get; private set; }

	[ObservableProperty] public partial bool IsConsoleVisible { get; set; }
	[ObservableProperty] public partial bool IsPhotoMode { get; private set; }
	[ObservableProperty] public partial bool IsPhotoModeHintShown { get; private set; }

	// Photo mode stops the UI rendering once its hint has faded out
	[ObservableProperty] public partial bool IsRendered { get; private set; } = true;

	protected override void OnDispose()
	{
		GSessionManager.SessionStarted -= OnSessionStarted;
		GSessionManager.SessionStopped -= OnSessionStopped;

		_dispatcher.UiProcess -= OnUiProcess;
		_dispatcher.Input -= OnInput;
		_dispatcher.Notification -= OnNotification;

		_photoModeTimer.Stop();
		_photoModeTimer.Tick -= OnPhotoModeTimerTick;

		Main = null;
		Stats = null;
		Clock = null;
		Toasts = null;
		IsConsoleVisible = false;
	}

	// Forces Godot to keep drawing out of session, where there's no 3D scene that would keep the UI updating
	private static void OnUiProcess(double delta) => RenderingServer.ForceDraw();

	private void OnSessionStarted()
	{
		_dispatcher.UiProcess -= OnUiProcess;
		OS.LowProcessorUsageMode = false;
		Log.Debug("Stopped forced render drawing");

		Main = _services.Create<GameViewModel>();
	}

	private void OnSessionStopped()
	{
		IsPhotoMode = false;
		_dispatcher.UiProcess += OnUiProcess;

		// Menus only change when the UI does, and the main loop would otherwise spin as fast as it can between
		OS.LowProcessorUsageMode = true;
		Log.Debug("Started forced render drawing");

		Main = _services.Create<MenuViewModel>();
	}

	private void OnInput(InputEvent @event)
	{
		if (InputSink.IsSunk && @event is InputEventKey { Unicode: not 0 })
			return;

		if (Input.IsActionJustPressedByEvent(ToggleConsoleAction, @event))
			IsConsoleVisible = !IsConsoleVisible;
		else if (Input.IsActionJustPressedByEvent(OpenTerminalAction, @event))
			OpenTerminal();
		// Only a session has a world to show without the UI
		else if (Input.IsActionJustPressedByEvent(TogglePhotoModeAction, @event) && GSessionManager.IsActive)
			IsPhotoMode = !IsPhotoMode;
	}

	private void OnNotification(int what)
	{
		if (what != Node.NotificationWMCloseRequest)
			return;

		foreach (var window in _terminals.ToArray())
			window.Close();
	}

	// Every toggle starts over, so no stage of a left photo mode carries on
	partial void OnIsPhotoModeChanged(bool value)
	{
		_photoModeTimer.Stop();
		IsPhotoModeHintShown = value;
		IsRendered = true;

		if (!value)
			return;

		_photoModeTimer.Interval = PhotoModeHintDuration;
		_photoModeTimer.Start();
	}

	// The hint holds, then fades out, and then nothing is left to draw
	private void OnPhotoModeTimerTick(object? sender, EventArgs e)
	{
		if (IsPhotoModeHintShown)
		{
			IsPhotoModeHintShown = false;
			_photoModeTimer.Interval = AnimationDuration;

			return;
		}

		_photoModeTimer.Stop();
		IsRendered = false;
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

	[RelayCommand]
	private void OpenTerminal()
	{
		var app = Application.Current!;
		var isDark = app.ActualThemeVariant == ThemeVariant.Dark;

		// Sync the appearance to the main UI
		var fontFamily = app.FindResource("Font") as FontFamily ?? FontFamily.Default;
		var fontSize = app.FindResource("FontSize") as double? ?? 16d;

		var accent =
			app.FindResource("HighlightBrush") as ISolidColorBrush ??
			(ISolidColorBrush)app.FindResource("PrimaryBrush")!;

		var selectionBrush = app.FindResource("ThemeAccentBrush3") as IBrush ?? new SolidColorBrush(accent.Color, 0.4);

		var terminal = new TerminalWindow
		{
			Width = TerminalWidth,
			Height = TerminalHeight,
			FontFamily = fontFamily,
			FontSize = fontSize,
			Foreground = isDark ? Brushes.White : Brushes.Black,
			Background = isDark ? Brushes.Black : Brushes.White,
			SelectionBrush = selectionBrush,
			Ligatures = true,
			CursorStyle = CursorStyle.Block,
			CursorColor = accent.Color,
			CursorBlink = true,
			CursorBlinkRate = TerminalCursorBlinkRateMs
		};

		Ui.ApplyAntialiasing(terminal);

		_terminals.Add(terminal);
		terminal.Closing += OnClosing;
		terminal.ProcessExited += OnProcessExited;

		Log.Debug("Created PTY process (Shell={Shell})", terminal.Process);

		terminal.Show();
		terminal.GetVisualDescendants().OfType<TerminalView>().FirstOrDefault()?.Focus();

		return;

		void OnClosing(object? sender, WindowClosingEventArgs e)
		{
			_terminals.Remove(terminal);
		}
	}

	private static void OnProcessExited(object? sender, ProcessExitedEventArgs e) =>
		Log.Debug("PTY process exited (ExitCode={ExitCode})", e.ExitCode);
}
