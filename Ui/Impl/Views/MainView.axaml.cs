using Avalonia.Controls;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class MainView : UserControl
{
	public MainView()
	{
		InitializeComponent();
		DataContext = new MainViewModel();
	}
}
