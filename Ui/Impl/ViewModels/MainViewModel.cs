using CommunityToolkit.Mvvm.ComponentModel;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Attributes;
using Root.Ui.Impl.Services;
using Serilog;

namespace Root.Ui.Impl.ViewModels;

public partial class MainViewModel : ViewModelBase
{
	private readonly DispatcherService _dispatcher;
	private readonly IServiceProvider _services;

	public MainViewModel(IServiceProvider services, DispatcherService dispatcher)
	{
		_services = services;
		_dispatcher = dispatcher;

		Stats = services.GetRequiredService<StatViewModel>();

		dispatcher.Input += OnInput;

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

		_dispatcher.Process -= OnProcess;
		_dispatcher.Input -= OnInput;

		Main = null;
		Stats = null;
		IsConsoleVisible = false;
	}

	private static void OnProcess(double delta) => RenderingServer.ForceDraw();

	private void OnSessionStarted()
	{
		_dispatcher.Process -= OnProcess;

		Log.Debug("Stopped forced render drawing.");

		Main = _services.GetRequiredService<GameViewModel>();
	}

	private void OnSessionStopped()
	{
		_dispatcher.Process += OnProcess;

		Log.Debug("Started forced render drawing...");

		Main = _services.GetRequiredService<MenuViewModel>();
	}

	private void OnInput(InputEvent @event)
	{
		if (Input.IsActionJustPressedByEvent("ui_toggle_console", @event))
			IsConsoleVisible = !IsConsoleVisible;
	}

	partial void OnIsConsoleVisibleChanging(bool value)
	{
		if (value)
		{
			Console = _services.GetRequiredService<ConsoleViewModel>();
			Log.Debug("Opened {Control}.", nameof(ConsoleViewModel));
		}
		else
		{
			Console = null;
			Log.Debug("Closed {Control}.", nameof(ConsoleViewModel));
		}
	}
}
