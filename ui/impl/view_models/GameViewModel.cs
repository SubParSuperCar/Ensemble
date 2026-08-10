using CommunityToolkit.Mvvm.ComponentModel;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Attributes;
using Root.Ui.Impl.Services;

namespace Root.Ui.Impl.ViewModels;

public partial class GameViewModel : ViewModelBase
{
	private readonly DispatcherService _dispatcher;
	private readonly IServiceProvider _services;

	public GameViewModel(IServiceProvider services, DispatcherService dispatcher)
	{
		_services = services;
		_dispatcher = dispatcher;

		Clock = services.GetRequiredService<ClockViewModel>();
		PlayerList = services.GetRequiredService<PlayerListViewModel>();
		PlotSelector = services.GetRequiredService<PlotSelectorViewModel>();

		dispatcher.Input += OnInput;
	}

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial ClockViewModel? Clock { get; set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial PlayerListViewModel? PlayerList { get; set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial PlotSelectorViewModel? PlotSelector { get; set; }

	protected override void OnDispose()
	{
		_dispatcher.Input -= OnInput;

		Clock = null;
		PlayerList = null;
		PlotSelector = null;
	}

	private void OnInput(InputEvent @event)
	{
		if (Input.IsActionJustPressedByEvent("ui_toggle_player_list", @event))
			PlayerList = PlayerList is null ? _services.GetRequiredService<PlayerListViewModel>() : null;
	}
}
