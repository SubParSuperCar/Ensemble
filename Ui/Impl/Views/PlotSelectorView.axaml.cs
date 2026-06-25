using Avalonia.Controls;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class PlotSelectorView : UserControl
{
	public PlotSelectorView()
	{
		InitializeComponent();
		DataContext = new PlotSelectorViewModel();
	}
}
