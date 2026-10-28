using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Scripts.Sessions;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Services;
using static EnsembleRoot.Sessions.SessionManager;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class SinglePlayerConfigViewModel(NavigatorService navigator) : ViewModelBase
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
