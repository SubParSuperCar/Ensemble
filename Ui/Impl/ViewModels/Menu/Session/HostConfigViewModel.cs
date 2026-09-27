using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;
using EnsembleRoot.Ui.Impl.ViewModels.Utils;
using static EnsembleRoot.SessionManager.SessionManager;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class HostConfigViewModel : ViewModelBase
{
	private readonly NavigatorService _navigator;

	public HostConfigViewModel(NavigatorService navigator)
	{
		_navigator = navigator;
		GSessionManager.SessionFailed += OnSessionFailed;
	}

	[ObservableProperty] public partial decimal? Port { get; set; }
	[ObservableProperty] public partial string? Password { get; set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(HostCommand))]
	public partial string? DisplayName { get; set; } = SessionPreferences.DisplayName;

	[ObservableProperty] public partial decimal? MaxClients { get; set; }

	[ObservableProperty] public partial string? Status { get; set; }

	protected override void OnDispose() => GSessionManager.SessionFailed -= OnSessionFailed;

	[RelayCommand]
	private void GoBack() => _navigator.GoBack();

	[RelayCommand(CanExecute = nameof(CanHost))]
	private void Host()
	{
		Status = null;
		SessionPreferences.DisplayName = DisplayName ?? string.Empty;

		GSessionManager.HostMultiPlayer(
			(int)(Port ?? DefaultPort),
			Password,
			DisplayName,
			(int?)MaxClients ?? Unlimited);
	}

	private bool CanHost() => IsValidDisplayName(DisplayName);

	private void OnSessionFailed(string reason) => Status = reason;
}
