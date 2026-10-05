using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Attributes;
using EnsembleRoot.Ui.Impl.Services;
using Godot;
using Microsoft.Extensions.DependencyInjection;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class GameViewModel : ViewModelBase
{
	private readonly DispatcherService _dispatcher;
	private readonly IServiceScope _scope;
	private readonly IServiceProvider _services;

	public GameViewModel(IServiceProvider services, DispatcherService dispatcher)
	{
		_services = services;
		_scope = services.CreateScope();
		_dispatcher = dispatcher;

		Widgets = _scope.ServiceProvider.GetRequiredService<WidgetManagerService>();
		Widgets.Register<PlotSelectorViewModel>();
		Widgets.Register<AssetSelectorViewModel>();
		Widgets.Open<PlotSelectorViewModel>();

		WidgetDrawer = _scope.ServiceProvider.GetRequiredService<WidgetDrawerViewModel>();

		Clock = services.GetRequiredService<ClockViewModel>();
		PlayerList = services.GetRequiredService<PlayerListViewModel>();

		dispatcher.Input += OnInput;

		OnIsLocalPlotSpawnedChanged(IsLocalPlotSpawned);
		IsLocalPlotSpawnedChanged += OnIsLocalPlotSpawnedChanged;

		OnConstructToolIsEnabledChanged(GToolManager.Construct.IsEnabled);
		GToolManager.Construct.IsEnabledChanged += OnConstructToolIsEnabledChanged;
	}

	public WidgetManagerService Widgets { get; }
	public WidgetDrawerViewModel WidgetDrawer { get; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial ClockViewModel? Clock { get; set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial PlayerListViewModel? PlayerList { get; set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial ToolBarViewModel? ToolBar { get; set; }


	protected override void OnDispose()
	{
		IsLocalPlotSpawnedChanged -= OnIsLocalPlotSpawnedChanged;
		GToolManager.Construct.IsEnabledChanged -= OnConstructToolIsEnabledChanged;

		_dispatcher.Input -= OnInput;

		Clock = null;
		PlayerList = null;
		ToolBar = null;

		_scope.Dispose();
	}

	private void OnInput(InputEvent @event)
	{
		if (!InputSink.IsSunk && Input.IsActionJustPressedByEvent("ui_toggle_player_list", @event))
			PlayerList = PlayerList is null ? _services.GetRequiredService<PlayerListViewModel>() : null;
	}

	private void OnIsLocalPlotSpawnedChanged(bool? isSpawned) =>
		ToolBar = isSpawned is false ? _services.GetRequiredService<ToolBarViewModel>() : null;

	private void OnConstructToolIsEnabledChanged(bool isEnabled)
	{
		if (isEnabled)
			Widgets.Open<AssetSelectorViewModel>();
		else
			Widgets.Close<AssetSelectorViewModel>();
	}
}
