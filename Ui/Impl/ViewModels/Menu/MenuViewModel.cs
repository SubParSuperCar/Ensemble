using EnsembleRoot.Common.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed class MenuViewModel : ViewModelBase
{
	private static readonly StringName BackAction = "ui_back";

	private readonly DispatcherService _dispatcher;
	private readonly IServiceScope _scope;

	public MenuViewModel(IServiceProvider services, DispatcherService dispatcher)
	{
		_scope = services.CreateScope();
		_dispatcher = dispatcher;

		Navigator = _scope.ServiceProvider.GetRequiredService<NavigatorService>();
		Navigator.GoTo<MenuHomeViewModel>();

		dispatcher.Input += OnInput;
	}

	public NavigatorService Navigator { get; }

	protected override void OnDispose()
	{
		_dispatcher.Input -= OnInput;

		Navigator.GoTo();
		_scope.Dispose();
	}

	private void OnInput(InputEvent @event)
	{
		if (!@event.IsActionPressed(BackAction) || !Navigator.CanGoBack || InputSink.IsSunk)
			return;

		Log.Debug("Navigating back from {ViewModel}...", Navigator.Current?.GetType().Name);
		Navigator.GoBack();

		Log.Debug("Navigated back to {ViewModel}", Navigator.Current?.GetType().Name);
	}
}
