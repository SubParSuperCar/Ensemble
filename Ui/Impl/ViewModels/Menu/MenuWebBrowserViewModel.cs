using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Attributes;
using EnsembleRoot.Ui.Extensions;
using EnsembleRoot.Ui.Services;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class MenuWebBrowserViewModel : ViewModelBase
{
	private readonly NavigatorService _navigator;

	public MenuWebBrowserViewModel(IServiceProvider services, NavigatorService navigator)
	{
		_navigator = navigator;

		Content = services.Create<WebBrowserViewModel>();
	}

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial WebBrowserViewModel? Content { get; private set; }

	protected override void OnDispose() => Content = null;

	[RelayCommand]
	private void GoBack() => _navigator.GoBack();
}
