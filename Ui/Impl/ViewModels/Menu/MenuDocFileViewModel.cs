using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Attributes;
using EnsembleRoot.Ui.Impl.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class MenuDocFileViewModel : ViewModelBase
{
	private readonly NavigatorService _navigator;

	public MenuDocFileViewModel(IServiceProvider services, NavigatorService navigator)
	{
		_navigator = navigator;
		Content = services.GetRequiredService<DocFileViewModel>();
	}

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial DocFileViewModel? Content { get; set; }

	protected override void OnDispose() => Content = null;

	[RelayCommand]
	private void GoBack() => _navigator.GoBack();
}
