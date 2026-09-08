using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Attributes;
using Root.Ui.Impl.Services;

namespace Root.Ui.Impl.ViewModels;

public partial class MenuWebBrowserViewModel : ViewModelBase
{
	private readonly NavigatorService _navigator;

	public MenuWebBrowserViewModel(IServiceProvider services, NavigatorService navigator)
	{
		_navigator = navigator;
		Content = services.GetRequiredService<WebBrowserViewModel>();
	}

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial WebBrowserViewModel? Content { get; set; }

	protected override void OnDispose() => Content = null;

	[RelayCommand]
	private void GoBack() => _navigator.GoBack();
}
