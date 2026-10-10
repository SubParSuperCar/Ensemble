using Avalonia;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Execution;
using EnsembleRoot.Ui.Impl.Abstractions;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed partial class LuaEditorViewModel : ViewModelBase, IWidget
{
	private static CancellationTokenSource _cts = new();

	public static TextDocument Source { get; } = new(
		"--[[\nLua 5.2\nReference Manual: https://www.lua.org/manual/5.2/\n" +
		"(Powered by: Lua-CSharp, AvaloniaEdit, & TextMate) ]]\n\n" +
		"print(string.format(\"Hello, %s!\", _VERSION))\nhelp()\n");

	public static WidgetDescriptor Descriptor { get; } = new(WidgetDescriptor.Cells(9, 3, 4, 6))
	{
		Description = "Lets you write and run Lua scripts, sharing the console's source.",
		MinSize = new Size(384, 256)
	};

	[RelayCommand]
	private static void Execute() => _ = LuaExecutor.ExecuteAsync(Source.Text, _cts.Token);

	[RelayCommand]
	private static async Task CancelAsync()
	{
		using var cts = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
		await cts.CancelAsync().ConfigureAwait(false);
	}
}
