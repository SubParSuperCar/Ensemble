using Avalonia.Controls;
using Avalonia.Input;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class MenuHomeView : UserControl, IViewFor<MenuHomeViewModel>
{
	public MenuHomeView()
	{
		InitializeComponent();
	}

	private void OnQuitButtonDoubleTapped(object? sender, TappedEventArgs e) => GMain.Quit();
}
