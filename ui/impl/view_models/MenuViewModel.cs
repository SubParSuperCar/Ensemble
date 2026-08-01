using Godot;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Services;
using Serilog;

namespace Root.Ui.Impl.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public class MenuViewModel : ViewModelBase
{
	private readonly DispatcherService _dispatcher;
	private readonly IServiceScope _scope;

	public MenuViewModel(IServiceProvider services, DispatcherService dispatcher)
	{
		_scope = services.CreateScope();
		_dispatcher = dispatcher;

		dispatcher.Input += OnInput;

		Navigator = _scope.ServiceProvider.GetRequiredService<NavigatorService>();
		Navigator.GoTo<LoadingIndicatorViewModel>(true);

		_ = GoToRootAfterDelay();
	}

	public NavigatorService Navigator { get; }

	private void OnInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_back") || !Navigator.CanGoBack)
			return;

		Log.Debug("Going back from {ViewModel}...", Navigator.Current?.GetType().Name);
		Navigator.GoBack();

		Log.Debug("Went back to {ViewModel}", Navigator.Current?.GetType().Name);
	}

	private async Task GoToRootAfterDelay()
	{
		await Task.Delay((int)(TimeSpan.MillisecondsPerSecond * 2.5)).ConfigureAwait(false);

		Navigator.GoTo<MenuHomeViewModel>();
	}

	protected override void OnDispose()
	{
		_dispatcher.Input -= OnInput;
		_scope.Dispose();
	}
}
