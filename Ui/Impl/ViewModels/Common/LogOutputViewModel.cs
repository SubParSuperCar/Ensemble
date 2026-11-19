using Avalonia;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Logging;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Services;
using Godot;
using Serilog.Events;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class LogOutputViewModel : ViewModelBase, IWidget
{
	private readonly DispatcherService _dispatcher;

	// The entries the output holds, in order, joined by line breaks
	private readonly List<LogEntry> _shownEntries = [];

	private byte _isHistoryDirty;

	public LogOutputViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;
		Output.UndoStack.SizeLimit = 0;

		dispatcher.UiProcess += OnUiProcess;

		OnLogHistoryUpdated();
		VolatileLogHistorySink.Updated += OnLogHistoryUpdated;
	}

	public static IReadOnlyList<LogEventLevel> Levels { get; } = Enum.GetValues<LogEventLevel>();

	/// <summary>The shown entries' text, edited as the history changes rather than rewritten.</summary>
	public TextDocument Output { get; } = new();

	[ObservableProperty] public partial LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Debug;

	public static WidgetDescriptor Descriptor { get; } = new(WidgetDescriptor.Cells(6, 6, 12, 18))
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
	partial void OnMinimumLevelChanged(LogEventLevel value)
	{
		_shownEntries.Clear();
		Output.Text = string.Empty;

		UpdateOutput();
	}

	private void OnLogHistoryUpdated() => Volatile.Write(ref _isHistoryDirty, 1);

	// Coalesces bursts of log entries into at most one refresh per UI frame
	private void OnUiProcess(double delta)
	{
		if (Interlocked.Exchange(ref _isHistoryDirty, 0) is 1)
			Dispatcher.UIThread.Post(UpdateOutput);
	}

	// Laying out the whole history again for each entry took long enough to stall frames, so the output is edited: the
	// history only drops its oldest entries and appends new ones, so one walk in order finds both
	private void UpdateOutput()
	{
		var entries = VolatileLogHistorySink.History.Where(entry => entry.Level >= MinimumLevel).ToList();
		var kept = 0;
		var offset = 0;

		Output.BeginUpdate();

		for (var index = 0; index < _shownEntries.Count;)
			if (kept < entries.Count && ReferenceEquals(entries[kept], _shownEntries[index]))
			{
				offset += _shownEntries[index].Text.Length + 1;
				kept++;
				index++;
			}
			else
				RemoveShownEntry(index, offset);

		foreach (var entry in entries.Skip(kept))
		{
			Output.Insert(Output.TextLength, _shownEntries.Count is 0 ? entry.Text : "\n" + entry.Text);
			_shownEntries.Add(entry);
		}

		Output.EndUpdate();
	}

	// With the line break between it and the next entry, or for the last one, the previous
	private void RemoveShownEntry(int index, int offset)
	{
		var length = _shownEntries[index].Text.Length;

		if (index < _shownEntries.Count - 1)
			Output.Remove(offset, length + 1);
		else if (index > 0)
			Output.Remove(offset - 1, length + 1);
		else
			Output.Remove(offset, length);

		_shownEntries.RemoveAt(index);
	}
}
