using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Services;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class ClockViewModel : ViewModelBase
{
	private static readonly string LocalTimeZone = TimeZoneInfo.Local.DisplayName;

	private readonly DispatcherService _dispatcher;

	private bool _isSessionShown;
	private long _shownSecond;

	public ClockViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;

		Refresh();
		dispatcher.UiProcess += OnUiProcess;
	}

	[ObservableProperty] public partial string Date { get; private set; } = string.Empty;
	[ObservableProperty] public partial string Time { get; private set; } = string.Empty;
	[ObservableProperty] public partial string Details { get; private set; } = string.Empty;

	protected override void OnDispose() => _dispatcher.UiProcess -= OnUiProcess;

	// Out of a session, nothing shown changes between seconds, until the one shown ends
	private void OnUiProcess(double delta)
	{
		if (
			GSessionManager.IsActive || _isSessionShown ||
			GTimeProvider.GetLocalNow().ToUnixTimeSeconds() != _shownSecond)
			Refresh();
	}

	private void Refresh()
	{
		var now = GTimeProvider.GetLocalNow();

		_shownSecond = now.ToUnixTimeSeconds();
		_isSessionShown = GSessionManager.IsActive;

		Date = string.Create(CultureInfo.CurrentCulture, $"{now:D} ");
		Time = string.Create(CultureInfo.CurrentCulture, $"{now:T}");
		Details = $" - {LocalTimeZone}";

		if (!GSessionManager.IsActive)
			return;

		var duration = (GTimeProvider.GetUtcNow() - GSessionManager.UtcStartedAt)
			.ToString(@"d\:hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);

		Details += $" - {duration}";
	}
}
