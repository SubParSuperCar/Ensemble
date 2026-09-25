using Avalonia.Controls;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public partial class MenuView : UserControl, IViewFor<MenuViewModel>
{
	public MenuView()
	{
		InitializeComponent();
	}
}
