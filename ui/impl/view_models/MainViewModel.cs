using CommunityToolkit.Mvvm.ComponentModel;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Attributes;
using Root.Ui.Impl.Services;
using Serilog;

namespace Root.Ui.Impl.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class MainViewModel : ViewModelBase
{
	private readonly DispatcherService _dispatcher;
	private readonly IServiceProvider _services;

	public MainViewModel(IServiceProvider services, DispatcherService dispatcher)
	{
		_services = services;
		_dispatcher = dispatcher;

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
	public partial ConsoleViewModel? Console { get; set; }

	protected override void OnDispose()
	{
		GSessionManager.SessionStarted -= OnSessionStarted;
		GSessionManager.SessionStopped -= OnSessionStopped;

		_dispatcher.Process -= OnProcess;
		_dispatcher.Input -= OnInput;

		Main = null;
		Console = null;
	}

	private void OnSessionStarted()
	{
		_dispatcher.Process -= OnProcess;

		Log.Debug("Stopped force render drawing");

		Main = _services.GetRequiredService<GameViewModel>();
	}

	private void OnSessionStopped()
	{
		_dispatcher.Process += OnProcess;

		Log.Debug("Started force render drawing...");

		Main = _services.GetRequiredService<MenuViewModel>();
	}

	private static void OnProcess(double delta) => RenderingServer.ForceDraw();

	private void OnInput(InputEvent @event)
	{
		if (!Input.IsActionJustPressedByEvent("toggle_console", @event))
			return;

		Console = Console is null ? _services.GetRequiredService<ConsoleViewModel>() : null;
		Log.Debug("Set console");
	}
}
