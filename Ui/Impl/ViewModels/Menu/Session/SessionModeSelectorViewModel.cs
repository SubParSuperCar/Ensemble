using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Services;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class SessionModeSelectorViewModel(NavigatorService navigator) : ViewModelBase
{
	[RelayCommand]
	private void GoToSinglePlayer() => navigator.GoTo<SinglePlayerConfigViewModel>();

	[RelayCommand]
	private void GoToMultiPlayer() => navigator.GoTo<MultiPlayerRoleSelectorViewModel>();

	[RelayCommand]
	private void GoBack() => navigator.GoBack();
}
