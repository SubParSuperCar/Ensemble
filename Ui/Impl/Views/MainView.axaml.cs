using Avalonia.Controls;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public partial class MainView : UserControl, IViewFor<MainViewModel>
{
	public MainView()
	{
		InitializeComponent();
	}
}
