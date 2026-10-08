using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed partial class SessionModeSelectorViewModel(NavigatorService navigator) : ViewModelBase
{
	[RelayCommand]
	private void GoToSinglePlayer() => navigator.GoTo<SinglePlayerConfigViewModel>();

	[RelayCommand]
	private void GoToMultiPlayer() => navigator.GoTo<MultiPlayerRoleSelectorViewModel>();

	[RelayCommand]
	private void GoBack() => navigator.GoBack();
}
