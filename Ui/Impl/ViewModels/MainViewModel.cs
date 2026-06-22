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

	[ObservableProperty] public partial PlayerListView? PlayerListView { get; set; } = new();

	public void Dispose()
	{
		Dispatcher.Input -= OnInput;

		GC.SuppressFinalize(this);
	}

	private void OnInput(InputEvent @event)
	{
		if (Input.IsActionJustPressedByEvent("toggle_player_list", @event))
			PlayerListView = PlayerListView is null ? new PlayerListView() : null;
	}
}
