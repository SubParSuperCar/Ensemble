using System.Text;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Root.Common.Execution;
using Root.Common.Logging;
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

	public TextDocument Source { get; } =
		new("--[[\nLua 5.2\n(Powered by: Lua-CSharp, AvaloniaEdit, & TextMate) ]]\n\nprint(\"Hello, World!\")\n");

	protected override void OnDispose() => LogHistorySinkVolatile.Updated -= OnLogHistoryUpdated;

	[RelayCommand]
	private void Execute() => _ = LuaExecutor.Execute(Source.Text);

	private void OnLogHistoryUpdated() =>
		Dispatcher.UIThread.Post(() =>
		{
			var history = LogHistorySinkVolatile.History;
			var sb = new StringBuilder(history.Count * 128);

			foreach (var line in history)
				sb.AppendLine(line);

			sb.Length -= Environment.NewLine.Length;
			Output = sb.ToString();
		});
}
