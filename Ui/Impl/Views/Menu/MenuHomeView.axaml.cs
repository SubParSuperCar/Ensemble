using Avalonia.Controls;
using Avalonia.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public partial class MenuHomeView : UserControl, IViewFor<MenuHomeViewModel>
{
	public MenuHomeView()
	{
		InitializeComponent();
	}

	private void OnQuitButtonDoubleTapped(object? sender, TappedEventArgs e) => GMain.Quit();
}
