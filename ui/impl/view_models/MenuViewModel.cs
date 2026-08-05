using Godot;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Services;
using Serilog;

namespace Root.Ui.Impl.ViewModels;

public class MenuViewModel : ViewModelBase
{
	private static bool _hasLoaded;
	private readonly DispatcherService _dispatcher;
	private readonly IServiceScope _scope;

	public MenuViewModel(IServiceProvider services, DispatcherService dispatcher)
	{
		_scope = services.CreateScope();
		_dispatcher = dispatcher;

		dispatcher.Input += OnInput;

		Navigator = _scope.ServiceProvider.GetRequiredService<NavigatorService>();

		if (_hasLoaded)
		{
			Navigator.GoTo<MenuHomeViewModel>();
			return;
		}

		Navigator.GoTo<LoadingIndicatorViewModel>(true);
		_ = GoToHomeAfterDelay();

		_hasLoaded = true;
	}

	public NavigatorService Navigator { get; }

	private void OnInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_back") || !Navigator.CanGoBack)
			return;

		Log.Debug("Navigating back from {ViewModel}...", Navigator.Current?.GetType().Name);
		Navigator.GoBack();

		Log.Debug("Navigated back to {ViewModel}", Navigator.Current?.GetType().Name);
	}

	private async Task GoToHomeAfterDelay()
	{
		await Task.Delay((int)(TimeSpan.MillisecondsPerSecond * 2.5)).ConfigureAwait(false);

		Navigator.GoTo<MenuHomeViewModel>();
	}

	protected override void OnDispose()
	{
		_dispatcher.Input -= OnInput;
		Navigator.GoTo();

		_scope.Dispose();
	}
}
