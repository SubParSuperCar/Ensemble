using CommunityToolkit.Mvvm.ComponentModel;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Attributes;
using Root.Ui.Impl.Services;

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

		if (GSessionManager.IsActive)
			OnSessionStarted();
		else
			OnSessionStopped();

		GSessionManager.SessionStarted += OnSessionStarted;
		GSessionManager.SessionStopped += OnSessionStopped;
	}

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	// ReSharper disable once UnusedMember.Global
	// ReSharper disable once MemberCanBeMadeStatic.Global
	public partial ViewModelBase? ViewModel { get; set; }

	protected override void OnDispose()
	{
		GSessionManager.SessionStarted -= OnSessionStarted;
		GSessionManager.SessionStopped -= OnSessionStopped;

		_dispatcher.Process -= OnProcess;

		ViewModel = null;
	}

	private void OnSessionStarted()
	{
		_dispatcher.Process -= OnProcess;

		Console.WriteLine("Stopped force render drawing");

		ViewModel = _services.GetRequiredService<GameViewModel>();
	}

	private void OnSessionStopped()
	{
		_dispatcher.Process += OnProcess;

		Console.WriteLine("Force render drawing...");

		ViewModel = _services.GetRequiredService<MenuViewModel>();
	}

	private static void OnProcess(double delta) => RenderingServer.ForceDraw();
}
