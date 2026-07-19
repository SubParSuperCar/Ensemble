using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using Root.Ui.Impl.Abstractions;

namespace Root.Ui.Impl.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class ConsoleViewModel : ViewModelBase
{
	// ReSharper disable once MemberCanBeMadeStatic.Global
	[ObservableProperty] public partial string Output { get; set; } = string.Empty;

	public TextDocument Source { get; } = new("Hello, World!\n");
}
