using Avalonia.Controls;
using Avalonia.Input;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class MenuHomeView : UserControl, IViewFor<MenuHomeViewModel>
{
	public MenuHomeView()
	{
		InitializeComponent();
	}

	private void OnQuitButtonDoubleTapped(object? sender, TappedEventArgs e) => GMain.Quit();
}
