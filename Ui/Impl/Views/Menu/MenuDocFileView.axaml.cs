using Avalonia.Controls;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class MenuDocFileView : UserControl, IViewFor<MenuDocFileViewModel>
{
	public MenuDocFileView()
	{
		InitializeComponent();
	}
}
