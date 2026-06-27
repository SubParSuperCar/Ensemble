using Avalonia.Controls;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class StatView : UserControl
{
	public StatView()
	{
		InitializeComponent();
		DataContext = new StatViewModel();
	}
}
