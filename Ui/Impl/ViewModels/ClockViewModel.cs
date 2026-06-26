using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Root.Ui.Impl.ViewModels;

public partial class ClockViewModel : ViewModelBase
{
	public ClockViewModel()
	{
		Dispatcher.Process += OnProcess;
	}

	// ReSharper disable once MemberCanBeMadeStatic.Global
	[ObservableProperty] public partial string Text { get; set; } = string.Empty;

	public override void Dispose()
	{
		Dispatcher.Process -= OnProcess;
		GC.SuppressFinalize(this);
	}

	private void OnProcess(double delta)
	{
		var startedAt = DateTimeOffset.FromUnixTimeSeconds((long)GHost.UtcStartedAtUnix);
		var sinceStarted = DateTimeOffset.UtcNow - startedAt;

		Text = string.Create(CultureInfo.InvariantCulture,
			$"{DateTime.Now:G} - {TimeZoneInfo.Local} - {sinceStarted:G}");
	}
}
