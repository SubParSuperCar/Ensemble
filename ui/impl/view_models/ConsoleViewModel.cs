using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using Root.Ui.Impl.Abstractions;

namespace Root.Ui.Impl.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
// TODO
public partial class ConsoleViewModel : ViewModelBase
{
	[ObservableProperty] public partial string Output { get; set; } = GPlots.GetAllDicts().ToString();

	public TextDocument Source { get; } = new("-- Lua 5.4\nprint(\"Hello, World!\")\n");
}
