using CommunityToolkit.Mvvm.ComponentModel;
using Godot;

// ReSharper disable MemberCanBeMadeStatic.Global

namespace Root.Ui.Impl.ViewModels;

public partial class MainViewModel : ViewModelBase
{
	public MainViewModel()
	{
		Dispatcher.Input += OnInput;
	}

	// ReSharper disable once UnusedMember.Global
	[ObservableProperty] public partial ClockViewModel? Clock { get; set; } = new();
	[ObservableProperty] public partial PlotSelectorViewModel? PlotSelector { get; set; } = new();
	[ObservableProperty] public partial PlayerListViewModel? PlayerList { get; set; } = new();

	public override void Dispose()
	{
		Dispatcher.Input -= OnInput;

		Clock = null;
		PlotSelector = null;
		PlayerList = null;

		GC.SuppressFinalize(this);
	}

	private void OnInput(InputEvent @event)
	{
		if (Input.IsActionJustPressedByEvent("toggle_player_list", @event))
			PlayerList = PlayerList is null ? new PlayerListViewModel() : null;
	}

	partial void OnClockChanging(ClockViewModel? oldValue, ClockViewModel? newValue) => OnChanging(oldValue, newValue);

	partial void OnPlayerListChanging(PlayerListViewModel? oldValue, PlayerListViewModel? newValue) =>
		OnChanging(oldValue, newValue);

	partial void OnPlotSelectorChanging(PlotSelectorViewModel? oldValue, PlotSelectorViewModel? newValue) =>
		OnChanging(oldValue, newValue);

	private static void OnChanging(IDisposable? oldValue, IDisposable? _) => oldValue?.Dispose();
}
