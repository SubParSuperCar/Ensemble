using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Attributes;
using EnsembleRoot.Ui.Extensions;
using EnsembleRoot.Ui.Services;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class MenuDocFileViewModel(IServiceProvider services, NavigatorService navigator) : ViewModelBase
{
	[ObservableProperty]
	[DisposeOldObservableValueOnChanging]
	public partial DocFileViewModel? Content { get; private set; } = services.Create<DocFileViewModel>();

	protected override void OnDispose() => Content = null;

	[RelayCommand]
	private void GoBack() => navigator.GoBack();
}
