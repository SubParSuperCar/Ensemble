using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Tooling.Tools;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Attributes;
using EnsembleRoot.Ui.Impl.Extensions;
using EnsembleRoot.Ui.Impl.Services;
using Godot;
using Microsoft.Extensions.DependencyInjection;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed partial class GameViewModel : ViewModelBase
{
	private static readonly StringName TogglePlayerListAction = "ui_toggle_player_list";

	private readonly DispatcherService _dispatcher;
	private readonly IServiceScope _scope;
	private readonly IServiceProvider _services;

	public GameViewModel(IServiceProvider services, DispatcherService dispatcher)
	{
		_services = services;
		_scope = services.CreateScope();
		_dispatcher = dispatcher;

		Widgets = _scope.ServiceProvider.GetRequiredService<WidgetManagerService>();
		Widgets.RegisterAll();
		Widgets.Open<PlotSelectorViewModel>();

		WidgetDrawer = _scope.ServiceProvider.GetRequiredService<WidgetDrawerViewModel>();
		Clock = services.Create<ClockViewModel>();
		PlayerList = services.Create<PlayerListViewModel>();

		dispatcher.Input += OnInput;

		OnIsLocalPlotSpawnedChanged(IsLocalPlotSpawned);
		IsLocalPlotSpawnedChanged += OnIsLocalPlotSpawnedChanged;

		OnToolIsEnabledChanged(null, GToolManager.IsEnabled<ConstructTool>());
		GToolManager.ToolIsEnabledChanged += OnToolIsEnabledChanged;
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
		_dispatcher.Input -= OnInput;
		IsLocalPlotSpawnedChanged -= OnIsLocalPlotSpawnedChanged;
		GToolManager.ToolIsEnabledChanged -= OnToolIsEnabledChanged;

		Clock = null;
		PlayerList = null;
		ToolBar = null;

		_scope.Dispose();
	}

	private void OnInput(InputEvent @event)
	{
		if (!InputSink.IsSunk && Input.IsActionJustPressedByEvent(TogglePlayerListAction, @event))
			PlayerList = PlayerList is null ? _services.Create<PlayerListViewModel>() : null;
	}

	private void OnIsLocalPlotSpawnedChanged(bool? isSpawned) =>
		ToolBar = isSpawned is false ? _services.Create<ToolBarViewModel>() : null;

	private void OnToolIsEnabledChanged(ToolBase? tool, bool isEnabled)
	{
		if (tool is not (null or ConstructTool))
			return;

		if (isEnabled)
			Widgets.Open<AssetSelectorViewModel>();
		else
			Widgets.Close<AssetSelectorViewModel>();
	}
}
