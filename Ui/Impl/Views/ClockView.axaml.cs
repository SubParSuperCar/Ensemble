using Avalonia.Controls;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class ClockView : UserControl
{
	public ClockView()
	{
		InitializeComponent();
		DataContext = new ClockViewModel();
	}
}
