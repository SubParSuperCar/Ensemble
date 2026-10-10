using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Logging;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Messages;
using EnsembleRoot.Ui.Impl.Services;
using Godot;
using Serilog.Events;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed partial class LogOutputViewModel : ViewModelBase, IWidget
{
	private readonly DispatcherService _dispatcher;
	private byte _isHistoryDirty;

	public LogOutputViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;

		dispatcher.UiProcess += OnUiProcess;

		OnLogHistoryUpdated();
		VolatileLogHistorySink.Updated += OnLogHistoryUpdated;
	}

	public static IReadOnlyList<LogEventLevel> Levels { get; } = Enum.GetValues<LogEventLevel>();

	[ObservableProperty] public partial string Output { get; private set; } = "<Default>";
	[ObservableProperty] public partial LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Debug;

	public static WidgetDescriptor Descriptor { get; } = new(WidgetDescriptor.Cells(3, 3, 6, 9))
	{
		Description = "Shows the recent log output, filtered by minimum severity level.",
		MinSize = new Size(448, 256)
	};

	protected override void OnDispose()
	{
		VolatileLogHistorySink.Updated -= OnLogHistoryUpdated;
		_dispatcher.UiProcess -= OnUiProcess;
	}

	[RelayCommand]
	private static void OpenUserDataDir() => OS.ShellOpen(ProjectSettings.GlobalizePath(UserScheme));

	// ReSharper disable once UnusedParameterInPartialMethod
	partial void OnMinimumLevelChanged(LogEventLevel value) => UpdateOutput();

	private void OnLogHistoryUpdated() => Volatile.Write(ref _isHistoryDirty, 1);

	// Coalesces bursts of log entries into at most one refresh per UI frame
	private void OnUiProcess(UiProcessData data)
	{
		if (Interlocked.Exchange(ref _isHistoryDirty, 0) is 1)
			Dispatcher.UIThread.Post(UpdateOutput);
	}

	private void UpdateOutput()
	{
		var minimumLevel = MinimumLevel;
		var entries = VolatileLogHistorySink.History.Where(entry => entry.Level >= minimumLevel);
		var text = string.Join('\n', entries.Select(static entry => entry.Text));

		Output = text.Length is 0 ? "<Empty>" : text;
	}
}
