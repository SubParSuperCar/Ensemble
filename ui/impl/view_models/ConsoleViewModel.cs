using System.Text;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using Root.Globals.Log;
using Root.Ui.Impl.Abstractions;

namespace Root.Ui.Impl.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
// TODO
public partial class ConsoleViewModel : ViewModelBase
{
	public ConsoleViewModel()
	{
		OnLogHistoryUpdated();
		LogHistorySinkVolatile.Updated += OnLogHistoryUpdated;
	}

	// ReSharper disable once MemberCanBeMadeStatic.Global
	[ObservableProperty] public partial string Output { get; set; } = string.Empty;

	public TextDocument Source { get; } = new("-- Lua 5.4\nprint(\"Hello, Ensemble!\")\n");

	protected override void OnDispose() => LogHistorySinkVolatile.Updated -= OnLogHistoryUpdated;

	private void OnLogHistoryUpdated() =>
		Dispatcher.UIThread.Post(() =>
		{
			var history = LogHistorySinkVolatile.History;
			var sb = new StringBuilder(history.Count * 128);

			foreach (var line in history)
				sb.AppendLine(line);

			sb.Length--;
			Output = sb.ToString();
		});
}
