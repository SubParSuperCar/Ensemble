using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;
using EnsembleRoot.Ui.Impl.ViewModels.Utils;
using static EnsembleRoot.SessionManager.SessionManager;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class JoinConfigViewModel : ViewModelBase
{
	private readonly NavigatorService _navigator;

	public JoinConfigViewModel(NavigatorService navigator)
	{
		_navigator = navigator;
		GSessionManager.SessionFailed += OnSessionFailed;
	}

	[ObservableProperty] public partial bool IsCodeMethod { get; set; }

	[ObservableProperty] public partial string? Address { get; set; }
	[ObservableProperty] public partial decimal? Port { get; set; }
	[ObservableProperty] public partial string? Code { get; set; }

	[ObservableProperty] public partial string? Password { get; set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(JoinCommand))]
	public partial string? DisplayName { get; set; } = SessionPreferences.DisplayName;

	[ObservableProperty] public partial string? Status { get; set; }

	protected override void OnDispose()
	{
		GSessionManager.SessionFailed -= OnSessionFailed;

		if (!GSessionManager.IsActive)
			GSessionManager.StopSession();
	}

	[RelayCommand]
	private void GoBack() => _navigator.GoBack();

	[RelayCommand(CanExecute = nameof(CanJoin))]
	private void Join()
	{
		if (GetEndPoint() is not var (address, port))
		{
			Status = "Invalid address or port.";
			return;
		}

		Status = "Joining\u2026";
		SessionPreferences.DisplayName = DisplayName ?? string.Empty;

		GSessionManager.JoinMultiPlayer(address, port, Password, DisplayName);
	}

	private bool CanJoin() => IsValidDisplayName(DisplayName);

	private (string Address, int Port)? GetEndPoint()
	{
		if (!IsCodeMethod)
			return string.IsNullOrWhiteSpace(Address) || Port is not { } port ? null : (Address.Trim(), (int)port);

		return HostEndPoint.TryParse(Code, out var endPoint) ? (endPoint.Host, endPoint.Port) : null;
	}

	private void OnSessionFailed(string reason) => Status = reason;
}
