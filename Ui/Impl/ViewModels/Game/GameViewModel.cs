using CommunityToolkit.Mvvm.ComponentModel;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Root.Common.Input;
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

		OnIsLocalPlotSpawnedChanged(IsLocalPlotSpawned);
		IsLocalPlotSpawnedChanged += OnIsLocalPlotSpawnedChanged;

		OnConstructToolIsEnabledChanged(GToolManager.Construct.IsEnabled);
		GToolManager.Construct.IsEnabledChanged += OnConstructToolIsEnabledChanged;
	}

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial ClockViewModel? Clock { get; set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial PlayerListViewModel? PlayerList { get; set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial ToolBarViewModel? ToolBar { get; set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial PlotSelectorViewModel? PlotSelector { get; set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial AssetSelectorViewModel? AssetSelector { get; set; }

	protected override void OnDispose()
	{
		IsLocalPlotSpawnedChanged -= OnIsLocalPlotSpawnedChanged;
		GToolManager.Construct.IsEnabledChanged -= OnConstructToolIsEnabledChanged;

		_dispatcher.Input -= OnInput;

		Clock = null;
		PlayerList = null;
		ToolBar = null;
		PlotSelector = null;
		AssetSelector = null;
	}

	private void OnInput(InputEvent @event)
	{
		if (!InputSink.IsSunk && Input.IsActionJustPressedByEvent("ui_toggle_player_list", @event))
			PlayerList = PlayerList is null ? _services.GetRequiredService<PlayerListViewModel>() : null;
	}

	private void OnIsLocalPlotSpawnedChanged(bool? isSpawned) =>
		ToolBar = isSpawned is false ? _services.GetRequiredService<ToolBarViewModel>() : null;

	private void OnConstructToolIsEnabledChanged(bool isEnabled) =>
		AssetSelector = isEnabled ? _services.GetRequiredService<AssetSelectorViewModel>() : null;
}
