using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Execution;
using EnsembleRoot.Ui.Impl.Abstractions;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class LuaEditorViewModel(TextDocument source) : ViewModelBase
{
	private static CancellationTokenSource _cts = new();

	public TextDocument Source { get; } = source;

	[RelayCommand]
	private void Execute() => _ = LuaExecutor.ExecuteAsync(Source.Text, _cts.Token);

	[RelayCommand]
	private static async Task CancelAsync()
	{
		using var cts = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
		await cts.CancelAsync().ConfigureAwait(false);
	}
}
