using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;
using static EnsembleRoot.SessionManager.SessionManager;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class SinglePlayerConfigViewModel(NavigatorService navigator) : ViewModelBase
{
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(StartCommand))]
	public partial string? DisplayName { get; set; }

	[RelayCommand]
	private void GoBack() => navigator.GoBack();

	[RelayCommand(CanExecute = nameof(CanStart))]
	private void Start() => GSessionManager.StartSinglePlayer(DisplayName);

	private bool CanStart() => IsValidDisplayName(DisplayName);
}
