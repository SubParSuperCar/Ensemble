using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Messages;
using EnsembleRoot.Ui.Impl.Services;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class ClockViewModel : ViewModelBase
{
	private static readonly string LocalTimeZone = TimeZoneInfo.Local.DisplayName;

	private readonly DispatcherService _dispatcher;

	public ClockViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;
		dispatcher.UiProcess += OnUiProcess;
	}

	[ObservableProperty] public partial string Text { get; set; } = "<Default>";

	protected override void OnDispose() => _dispatcher.UiProcess -= OnUiProcess;

	private void OnUiProcess(UiProcessData data)
	{
		var sessionDuration = GTimeProvider.GetUtcNow() - GSessionManager.UtcStartedAt;
		var duration = sessionDuration.ToString(@"d\:hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);

		Text = string.Create(
			CultureInfo.CurrentCulture,
			$"{GTimeProvider.GetLocalNow():F} - {LocalTimeZone} - {duration}");
	}
}
