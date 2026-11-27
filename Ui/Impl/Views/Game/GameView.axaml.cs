using Avalonia.Controls;
using Avalonia.Input;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class GameView : UserControl, IViewFor<GameViewModel>
{
	public GameView()
	{
		InitializeComponent();
	}

	private static async Task QuitSessionAsync()
	{
		if (await Main.ConfirmEndingSessionAsync("End Session?", "Quitting the session").ConfigureAwait(true))
			GSessionManager.StopSession();
	}

	private void OnMenuButtonDoubleTapped(object? sender, TappedEventArgs e) => _ = QuitSessionAsync();
}
