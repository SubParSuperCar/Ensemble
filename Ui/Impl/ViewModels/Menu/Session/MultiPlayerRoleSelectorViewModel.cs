using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Services;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class MultiPlayerRoleSelectorViewModel(NavigatorService navigator) : ViewModelBase
{
	[RelayCommand]
	private void GoToJoin() => navigator.GoTo<JoinConfigViewModel>();

	[RelayCommand]
	private void GoToHost() => navigator.GoTo<HostConfigViewModel>();

	[RelayCommand]
	private void GoBack() => navigator.GoBack();
}
