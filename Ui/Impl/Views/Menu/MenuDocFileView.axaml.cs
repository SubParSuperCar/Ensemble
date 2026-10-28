using Avalonia.Controls;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class MenuDocFileView : UserControl, IViewFor<MenuDocFileViewModel>
{
	public MenuDocFileView()
	{
		InitializeComponent();
	}
}
