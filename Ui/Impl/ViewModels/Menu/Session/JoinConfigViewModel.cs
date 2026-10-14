using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Common.Text;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;
using EnsembleRoot.Ui.Impl.ViewModels.Utils;
using static EnsembleRoot.Sessions.SessionManager;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed partial class JoinConfigViewModel : ViewModelBase
{
	private readonly NavigatorService _navigator;

	public JoinConfigViewModel(NavigatorService navigator)
	{
		_navigator = navigator;

		if (SessionPreferences.JoinEndPoint is { } endPoint)
		{
			Address = endPoint.Host;
			Port = endPoint.Port;
			Code = endPoint.ToString();
		}

		GSessionManager.SessionFailed += OnSessionFailed;
	}

	[ObservableProperty] public partial bool IsCodeMethod { get; set; } = SessionPreferences.IsJoinByCode;

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
			Status = IsCodeMethod ? "Invalid code." : "Invalid address or port.";
			return;
		}

		Status = "Joining\u2026";

		SessionPreferences.DisplayName = DisplayName ?? string.Empty;
		SessionPreferences.JoinEndPoint = new HostEndPoint(address, port);
		SessionPreferences.IsJoinByCode = IsCodeMethod;

		GSessionManager.JoinMultiPlayer(address, port, Password, DisplayName);
	}

	private bool CanJoin() => IsValidDisplayName(DisplayName);

	private (string Address, int Port)? GetEndPoint()
	{
		// Round-trips manual input through the parser, so it's validated like a code
		var text = IsCodeMethod
			? Code
			: string.IsNullOrWhiteSpace(Address) || Port is not { } port
				? null
				: new HostEndPoint(Address.Trim(), (int)port).ToString();

		return HostEndPoint.TryParse(text, out var endPoint) ? (endPoint.Host, endPoint.Port) : null;
	}

	private void OnSessionFailed(string reason) =>
		Status = TextElements.Truncate(reason, MenuHomeViewModel.MaxReasonLength);
}
