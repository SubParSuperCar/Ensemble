using System.Text;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Godot;
using Root.Common.Execution;
using Root.Common.Logging;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Services;
using Dispatcher = Avalonia.Threading.Dispatcher;
using Environment = System.Environment;

namespace Root.Ui.Impl.ViewModels;

public partial class ConsoleViewModel : ViewModelBase
{
	private static CancellationTokenSource _cts = new();
	private readonly DispatcherService _dispatcher;

	private byte _updateLogHistoryFlag;

	public ConsoleViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;
		dispatcher.Process += OnProcess;

		OnLogHistoryUpdated();
		VolatileLogHistorySink.Updated += OnLogHistoryUpdated;
	}

	[ObservableProperty] public partial string Output { get; set; } = string.Empty;

	public static TextDocument Source { get; } = new(
		"--[[\nLua 5.2\nReference Manual: https://www.lua.org/manual/5.2/\n" +
		"(Powered by: Lua-CSharp, AvaloniaEdit, & TextMate) ]]\n\n" +
		"print(string.format(\"Hello, %s!\", _VERSION))\nhelp()\n");

	protected override void OnDispose()
	{
		VolatileLogHistorySink.Updated -= OnLogHistoryUpdated;
		_dispatcher.Process -= OnProcess;
	}

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

	private void OnLogHistoryUpdated() => Volatile.Write(ref _updateLogHistoryFlag, 1);

	private void OnProcess(double delta)
	{
		if (Interlocked.Exchange(ref _updateLogHistoryFlag, 0) is 1)
			Dispatcher.UIThread.Post(UpdateOutput);
	}

	private void UpdateOutput()
	{
		var history = VolatileLogHistorySink.History;
		var builder = new StringBuilder(history.Count * 128);

		foreach (var line in history)
			builder.AppendLine(line);

		if (builder.Length > 0)
			builder.Length -= Environment.NewLine.Length;

		Output = builder.ToString();
	}
}
