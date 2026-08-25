using Godot;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Services;
using Serilog;

namespace Root.Ui.Impl.ViewModels;

public class MenuViewModel : ViewModelBase
{
	private readonly DispatcherService _dispatcher;
	private readonly IServiceScope _scope;

	public MenuViewModel(IServiceProvider services, DispatcherService dispatcher)
	{
		_scope = services.CreateScope();
		_dispatcher = dispatcher;

		Navigator = _scope.ServiceProvider.GetRequiredService<NavigatorService>();

		dispatcher.Input += OnInput;

		Navigator.GoTo<MenuHomeViewModel>();
	}

	public NavigatorService Navigator { get; }

	private void OnInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_back") || !Navigator.CanGoBack)
			return;

		Log.Debug("Navigating back from {ViewModel}...", Navigator.Current?.GetType().Name);
		Navigator.GoBack();

		Log.Debug("Navigated back to {ViewModel}.", Navigator.Current?.GetType().Name);
	}

	protected override void OnDispose()
	{
		_dispatcher.Input -= OnInput;
		Navigator.GoTo();

		_scope.Dispose();
	}
}
