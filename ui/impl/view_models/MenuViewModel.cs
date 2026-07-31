using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Services;

namespace Root.Ui.Impl.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public class MenuViewModel : ViewModelBase
{
	private readonly IServiceScope _scope;
	private bool _isDisposed;

	public MenuViewModel(IServiceProvider services)
	{
		_scope = services.CreateScope();

		Navigator = _scope.ServiceProvider.GetRequiredService<NavigatorService>();
		Navigator.GoTo<LoadingIndicatorViewModel>();

		_ = GoToRootAfterDelay();
	}

	public NavigatorService Navigator { get; }

	private async Task GoToRootAfterDelay()
	{
		do
		{
			const int msps = (int)TimeSpan.MillisecondsPerSecond;

			await Task.Delay((int)(msps * 2.5)).ConfigureAwait(false);
			Navigator.GoTo<ConsoleViewModel>();

			await Task.Delay(msps * 2).ConfigureAwait(false);
			Navigator.GoTo<StatViewModel>();

			await Task.Delay((int)(msps * 2.5)).ConfigureAwait(false);
			Navigator.GoTo<MenuRootViewModel>();

			await Task.Delay((int)(msps * 1.5)).ConfigureAwait(false);
			Navigator.GoTo<LoadingIndicatorViewModel>();
		} while (!_isDisposed);
	}

	protected override void OnDispose()
	{
		_scope.Dispose();
		_isDisposed = true;
	}
}
