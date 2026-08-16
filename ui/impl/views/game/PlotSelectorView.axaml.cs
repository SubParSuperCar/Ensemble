using Avalonia.Controls;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class PlotSelectorView : UserControl, IViewFor<PlotSelectorViewModel>
{
	public PlotSelectorView()
	{
		InitializeComponent();
	}
}
