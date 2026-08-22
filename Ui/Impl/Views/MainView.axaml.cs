using Avalonia.Controls;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

// This is basically a stub for now; I'll add more UI later on.
public partial class MainView : UserControl, IViewFor<MainViewModel>
{
	public MainView()
	{
		InitializeComponent();
	}
}
