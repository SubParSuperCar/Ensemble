using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Root.Ui.Impl.ViewModels;

public partial class ClockViewModel : ViewModelBase
{
	private static readonly string LocalTimeZone = TimeZoneInfo.Local.DisplayName;

	public ClockViewModel()
	{
		Dispatcher.Process += OnProcess;
	}

	// ReSharper disable once MemberCanBeMadeStatic.Global
	[ObservableProperty] public partial string Text { get; set; } = string.Empty;

	protected override void OnDispose() => Dispatcher.Process -= OnProcess;

	private void OnProcess(double delta)
	{
		var startedAt = DateTimeOffset.FromUnixTimeSeconds((long)GHost.UtcStartedAtUnix);

		Text = string.Create(CultureInfo.InvariantCulture,
			$"{DateTime.Now:G} - {LocalTimeZone} - {DateTimeOffset.UtcNow - startedAt:G}");
	}
}
