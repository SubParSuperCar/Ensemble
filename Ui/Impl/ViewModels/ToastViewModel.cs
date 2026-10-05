using System.Globalization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Logging;
using EnsembleRoot.Ui.Impl.Abstractions;
using Serilog.Events;

namespace EnsembleRoot.Ui.Impl.ViewModels;

/// <summary>A shown log entry, dismissing itself once its lifetime passes without repeats.</summary>
public sealed partial class ToastViewModel : ViewModelBase
{
	private readonly ToastListViewModel _owner;
	private readonly DispatcherTimer _timer;

	internal ToastViewModel(ToastListViewModel owner, LogEntry entry, TimeSpan lifetime)
	{
		_owner = owner;
		_timer = new DispatcherTimer(lifetime, DispatcherPriority.Normal, OnTimerTick);

		Entry = entry;
	}

	public LogEntry Entry { get; }

	public string LevelText =>
		Entry.Level switch
		{
			LogEventLevel.Verbose => "VRB",
			LogEventLevel.Debug => "DBG",
			LogEventLevel.Information => "INF",
			LogEventLevel.Warning => "WRN",
			LogEventLevel.Error => "ERR",
			_ => "FTL"
		};

	public string RepeatText => string.Create(CultureInfo.InvariantCulture, $"x{RepeatCount}");
	public bool IsRepeated => RepeatCount > 1;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(RepeatText), nameof(IsRepeated))]
	public partial int RepeatCount { get; private set; } = 1;

	[ObservableProperty] public partial bool IsDismissing { get; set; }

	internal bool IsRepeatOf(LogEntry entry) =>
		entry.Level == Entry.Level && string.Equals(entry.Message, Entry.Message, StringComparison.Ordinal);

	internal void Repeat()
	{
		RepeatCount++;

		_timer.Stop();
		_timer.Start();
	}

	protected override void OnDispose() => _timer.Stop();

	[RelayCommand]
	private void Dismiss() => _owner.Dismiss(this);

	private void OnTimerTick(object? sender, EventArgs e) => Dismiss();
}
