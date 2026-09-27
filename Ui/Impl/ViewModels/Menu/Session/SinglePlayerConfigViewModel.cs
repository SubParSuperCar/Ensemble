using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;
using EnsembleRoot.Ui.Impl.ViewModels.Utils;
using static EnsembleRoot.SessionManager.SessionManager;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class SinglePlayerConfigViewModel(NavigatorService navigator) : ViewModelBase
{
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(StartCommand))]
	public partial string? DisplayName { get; set; } = SessionPreferences.DisplayName;

	[RelayCommand]
	private void GoBack() => navigator.GoBack();

	[RelayCommand(CanExecute = nameof(CanStart))]
	private void Start()
	{
		SessionPreferences.DisplayName = DisplayName ?? string.Empty;
		GSessionManager.StartSinglePlayer(DisplayName);
	}

	private bool CanStart() => IsValidDisplayName(DisplayName);
}
