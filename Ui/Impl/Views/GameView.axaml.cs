using Avalonia.Controls;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class GameView : UserControl
{
	public GameView()
	{
		InitializeComponent();
		DataContext = new GameViewModel();
	}
}
