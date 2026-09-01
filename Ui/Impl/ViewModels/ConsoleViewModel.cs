using System.Text;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Godot;
using Root.Common.Execution;
using Root.Common.Logging;
using Root.Ui.Impl.Abstractions;
using Dispatcher = Avalonia.Threading.Dispatcher;
using Environment = System.Environment;

namespace Root.Ui.Impl.ViewModels;

public partial class ConsoleViewModel : ViewModelBase
{
	private static CancellationTokenSource _cts = new();

	public ConsoleViewModel()
	{
		OnLogHistoryUpdated();
		VolatileLogHistorySink.Updated += OnLogHistoryUpdated;
	}

	[ObservableProperty] public partial string Output { get; set; } = string.Empty;

	public static TextDocument Source { get; } = new(
		"--[[\nLua 5.2\nReference Manual: https://www.lua.org/manual/5.2/\n" +
		"(Powered by: Lua-CSharp, AvaloniaEdit, & TextMate) ]]\n\n" +
		"print(string.format(\"Hello, %s!\", _VERSION))\nhelp()\n");

	protected override void OnDispose() => VolatileLogHistorySink.Updated -= OnLogHistoryUpdated;

	[RelayCommand]
	private static void OpenUserDataDir() => OS.ShellOpen(ProjectSettings.GlobalizePath(UserScheme));

	[RelayCommand]
	private static void Execute() => _ = LuaExecutor.ExecuteAsync(Source.Text, _cts.Token);

	[RelayCommand]
	private static async Task CancelAsync()
	{
		using var cts = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
		await cts.CancelAsync().ConfigureAwait(false);
	}

	private void OnLogHistoryUpdated() =>
		Dispatcher.UIThread.Post(() =>
		{
			var history = VolatileLogHistorySink.History;
			var builder = new StringBuilder(history.Count * 128);

			foreach (var line in history)
				builder.AppendLine(line);

			if (builder.Length > 0)
				builder.Length -= Environment.NewLine.Length;

			Output = builder.ToString();
		});
}
