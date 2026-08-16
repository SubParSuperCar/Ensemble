using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Services;

namespace Root.Ui.Impl.ViewModels;

public partial class ClockViewModel : ViewModelBase
{
	private static readonly string LocalTimeZone = TimeZoneInfo.Local.DisplayName;
	private readonly DispatcherService _dispatcher;

	public ClockViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;
		dispatcher.Process += OnProcess;
	}

	[ObservableProperty] public partial string Text { get; set; } = string.Empty;

	protected override void OnDispose() => _dispatcher.Process -= OnProcess;

	private void OnProcess(double delta)
	{
		var startedAt = DateTimeOffset.FromUnixTimeSeconds((long)GSessionManager.UtcStartedAtUnix);

		Text = string.Create(CultureInfo.CurrentCulture,
			$"{DateTime.Now:F} - {LocalTimeZone} - {DateTimeOffset.UtcNow - startedAt:G}");
	}
}
