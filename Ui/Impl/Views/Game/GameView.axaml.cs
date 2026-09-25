using Avalonia.Controls;
using Avalonia.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public partial class GameView : UserControl, IViewFor<GameViewModel>
{
	public GameView()
	{
		InitializeComponent();
	}

	private void OnMenuButtonDoubleTapped(object? sender, TappedEventArgs e) => GSessionManager.StopSession();
}
