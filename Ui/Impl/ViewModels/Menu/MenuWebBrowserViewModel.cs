using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Attributes;
using EnsembleRoot.Ui.Extensions;
using EnsembleRoot.Ui.Services;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class MenuWebBrowserViewModel(IServiceProvider services, NavigatorService navigator)
	: ViewModelBase
{
	[ObservableProperty]
	[DisposeOldObservableValueOnChanging]
	public partial WebBrowserViewModel? Content { get; private set; } = services.Create<WebBrowserViewModel>();

	protected override void OnDispose() => Content = null;

	[RelayCommand]
	private void GoBack() => navigator.GoBack();
}
