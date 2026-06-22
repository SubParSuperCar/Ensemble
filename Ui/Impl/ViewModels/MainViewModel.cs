using CommunityToolkit.Mvvm.ComponentModel;
using Godot;
using Root.Ui.Impl.Views;

namespace Root.Ui.Impl.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
	public MainViewModel()
	{
		Dispatcher.Input += OnInput;
	}

	[ObservableProperty] public partial PlayerListViewModel? PlayerList { get; set; } = new();

	public void Dispose()
	{
		Dispatcher.Input -= OnInput;
		PlayerList = null;

		GC.SuppressFinalize(this);
	}

	private void OnInput(InputEvent @event)
	{
		if (Input.IsActionJustPressedByEvent("toggle_player_list", @event))
			PlayerList = PlayerList is null ? new PlayerListViewModel() : null;
	}

	partial void OnPlayerListChanging(PlayerListViewModel? oldValue, PlayerListViewModel? newValue)
	{
		_ = newValue;
		oldValue?.Dispose();
	}
}
