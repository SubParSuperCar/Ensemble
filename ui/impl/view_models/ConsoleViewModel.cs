using System.Text;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Root.Common.Execution;
using Root.Common.Logging;
using Root.Ui.Impl.Abstractions;

namespace Root.Ui.Impl.ViewModels;

public partial class ConsoleViewModel : ViewModelBase
{
	public ConsoleViewModel()
	{
		OnLogHistoryUpdated();
		LogHistorySinkVolatile.Updated += OnLogHistoryUpdated;
	}

	[ObservableProperty] public partial string Output { get; set; } = string.Empty;

	public static TextDocument Source { get; } =
		new("--[[\nLua 5.2\n(Powered by: Lua-CSharp, AvaloniaEdit, & TextMate) ]]\n\n" +
			"print(\"Hello, World!\")\n\nhelp()\n");

	protected override void OnDispose() => LogHistorySinkVolatile.Updated -= OnLogHistoryUpdated;

	[RelayCommand]
	private static void Execute() => _ = LuaExecutor.Execute(Source.Text);

	private void OnLogHistoryUpdated() =>
		Dispatcher.UIThread.Post(() =>
		{
			var history = LogHistorySinkVolatile.History;
			var builder = new StringBuilder(history.Count * 128);

			foreach (var line in history)
				builder.AppendLine(line);

			if (builder.Length > 0)
				builder.Length -= Environment.NewLine.Length;

			Output = builder.ToString();
		});
}
