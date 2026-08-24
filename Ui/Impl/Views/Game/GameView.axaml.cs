using Avalonia.Controls;
using Avalonia.Input;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class GameView : UserControl, IViewFor<GameViewModel>
{
	public GameView()
	{
		InitializeComponent();
	}

	private void Button_DoubleTapped(object? sender, TappedEventArgs e) => GSessionManager.StopSession();
}
