using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Root.Gd.Globals;

namespace Root.Ui.Impl.ViewModels;

public partial class ClockViewModel : ViewModelBase, IDisposable
{
	public ClockViewModel()
	{
		Dispatcher.Process += OnProcess;
	}

	// ReSharper disable once MemberCanBeMadeStatic.Global
	[ObservableProperty] public partial string Text { get; set; }

	public void Dispose()
	{
		Dispatcher.Process -= OnProcess;

		GC.SuppressFinalize(this);
	}

	private void OnProcess(double delta)
	{
		var startedAt = DateTimeOffset.FromUnixTimeSeconds((long)GdGlobals.Host.UtcStartedAtUnix);
		var now = DateTimeOffset.UtcNow;
		var sinceStarted = now - startedAt;

		Text = string.Create(CultureInfo.InvariantCulture,
			$"{DateTime.Now:G} - {TimeZoneInfo.Local} - {sinceStarted:G}");
	}
}
