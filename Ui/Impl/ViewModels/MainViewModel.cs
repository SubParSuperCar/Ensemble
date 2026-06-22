using CommunityToolkit.Mvvm.ComponentModel;
using Godot;

namespace Root.Ui.Impl.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
	public MainViewModel()
	{
		Dispatcher.Input += OnInput;
	}

	[ObservableProperty]
	// TODO: Fix bug
	public partial bool IsPlayerListVisible { get; set; }

	public void Dispose()
	{
		Dispatcher.Input -= OnInput;

		GC.SuppressFinalize(this);
	}

	private void OnInput(InputEvent @event)
	{
		if (Input.IsActionJustPressedByEvent("toggle_player_list", @event))
			IsPlayerListVisible = !IsPlayerListVisible;
	}
}
