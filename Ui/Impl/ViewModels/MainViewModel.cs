using CommunityToolkit.Mvvm.ComponentModel;

namespace Root.Ui.Impl.ViewModels;

public partial class MainViewModel : ViewModelBase
{
	// ReSharper disable once MemberCanBeMadeStatic.Global
	[ObservableProperty] public partial GameViewModel? Game { get; set; } = new();

	protected override void OnDispose() => Game = null;

	partial void OnGameChanging(GameViewModel? oldValue, GameViewModel? newValue)
	{
		_ = newValue;
		oldValue?.Dispose();
	}
}
