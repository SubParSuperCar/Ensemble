using Avalonia.Controls;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

// ReSharper disable once UnusedType.Global
public partial class StatView : UserControl
{
	public StatView()
	{
		InitializeComponent();
		DataContext = new StatViewModel();
	}
}
