using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Services;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class ClockViewModel : ViewModelBase
{
	private static readonly string LocalTimeZone = TimeZoneInfo.Local.DisplayName;

	private readonly DispatcherService _dispatcher;

	public ClockViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;

		Refresh();
		dispatcher.UiProcess += OnUiProcess;
	}

	[ObservableProperty] public partial string Text { get; private set; } = string.Empty;

	protected override void OnDispose() => _dispatcher.UiProcess -= OnUiProcess;

	private void OnUiProcess(double delta) => Refresh();

	// The session duration only exists during a session
	private void Refresh()
	{
		var time = string.Create(CultureInfo.CurrentCulture, $"{GTimeProvider.GetLocalNow():F} - {LocalTimeZone}");

		if (!GSessionManager.IsActive)
		{
			Text = time;
			return;
		}

		var duration = (GTimeProvider.GetUtcNow() - GSessionManager.UtcStartedAt)
			.ToString(@"d\:hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);

		Text = $"{time} - {duration}";
	}
}
