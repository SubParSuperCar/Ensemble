using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class MultiPlayerRoleSelectorViewModel(NavigatorService navigator) : ViewModelBase
{
	[RelayCommand]
	private void GoToJoin() => navigator.GoTo<JoinConfigViewModel>();

	[RelayCommand]
	private void GoToHost() => navigator.GoTo<HostConfigViewModel>();

	[RelayCommand]
	private void GoBack() => navigator.GoBack();
}
