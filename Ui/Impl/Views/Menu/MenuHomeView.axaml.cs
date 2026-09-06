using Avalonia.Controls;
using Avalonia.Input;
using Godot;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class MenuHomeView : UserControl, IViewFor<MenuHomeViewModel>
{
	public MenuHomeView()
	{
		InitializeComponent();
	}

	private void OnQuitButtonDoubleTapped(object? sender, TappedEventArgs e) =>
		(Engine.GetMainLoop() as SceneTree)?.Quit();
}
