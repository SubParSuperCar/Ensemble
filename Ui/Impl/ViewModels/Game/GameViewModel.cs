using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Scripts.Chat;
using EnsembleRoot.Sessions.Api;
using EnsembleRoot.Tooling.Tools;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Attributes;
using EnsembleRoot.Ui.Extensions;
using EnsembleRoot.Ui.Services;
using Godot;
using Microsoft.Extensions.DependencyInjection;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class GameViewModel : ViewModelBase
{
	private const DynamicallyAccessedMemberTypes Constructors = DynamicallyAccessedMemberTypes.PublicConstructors;

	private static readonly StringName TogglePlayerListAction = "ui_player_list_toggle";

	private readonly DispatcherService _dispatcher;
	private readonly IServiceScope _scope;
	private readonly IServiceProvider _services;
	private readonly WidgetEntry _textChat;

	public GameViewModel(IServiceProvider services, DispatcherService dispatcher)
	{
		_services = services;
		_scope = services.CreateScope();
		_dispatcher = dispatcher;

		Widgets = _scope.ServiceProvider.GetRequiredService<WidgetManagerService>();
		Widgets.RegisterAll();

		_textChat = Widgets.Register<TextChatViewModel>();
		_textChat.PropertyChanged += OnTextChatPropertyChanged;
		GChatManager.MessageAdded += OnChatMessageAdded;

		// Opened first, so the plot selector ends up active
		if (GSessionManager.Mode is SessionMode.MultiPlayer)
			Widgets.Open<TextChatViewModel>();

		Widgets.Open<PlotSelectorViewModel>();

		WidgetDrawer = _scope.ServiceProvider.GetRequiredService<WidgetDrawerViewModel>();
		PlayerList = services.Create<PlayerListViewModel>();
		Marquee = services.Create<MarqueeViewModel>();

		dispatcher.Input += OnInput;

		OnIsLocalPlotSpawnedChanged(IsLocalPlotSpawned);
		IsLocalPlotSpawnedChanged += OnIsLocalPlotSpawnedChanged;
		OnIsPlotOwnerChanged(IsPlotOwner);
		IsPlotOwnerChanged += OnIsPlotOwnerChanged;

		SetWidgetOpen<AssetSelectorViewModel>(GToolManager.IsEnabled<ConstructTool>());
		SetWidgetOpen<PropertyEditorViewModel>(GToolManager.IsEnabled<PropertyTool>());
		GToolManager.ToolIsEnabledChanged += OnToolIsEnabledChanged;
	}

	public WidgetManagerService Widgets { get; }
	public WidgetDrawerViewModel WidgetDrawer { get; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial PlayerListViewModel? PlayerList { get; private set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial ToolBarViewModel? ToolBar { get; private set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial SpawnButtonViewModel? SpawnButton { get; private set; }

	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial MarqueeViewModel? Marquee { get; private set; }

	protected override void OnDispose()
	{
		_textChat.PropertyChanged -= OnTextChatPropertyChanged;
		GChatManager.MessageAdded -= OnChatMessageAdded;

		_dispatcher.Input -= OnInput;

		IsLocalPlotSpawnedChanged -= OnIsLocalPlotSpawnedChanged;
		IsPlotOwnerChanged -= OnIsPlotOwnerChanged;
		GToolManager.ToolIsEnabledChanged -= OnToolIsEnabledChanged;

		PlayerList = null;
		ToolBar = null;
		SpawnButton = null;
		Marquee = null;

		_scope.Dispose();
	}

	private void OnInput(InputEvent @event)
	{
		if (!InputSink.IsSunk && Input.IsActionJustPressedByEvent(TogglePlayerListAction, @event))
			PlayerList = PlayerList is null ? _services.Create<PlayerListViewModel>() : null;
	}

	private void OnIsLocalPlotSpawnedChanged(bool? isSpawned) =>
		ToolBar = isSpawned is false ? ToolBar ?? _services.Create<ToolBarViewModel>() : null;

	private void OnIsPlotOwnerChanged(bool? isOwner) =>
		SpawnButton = isOwner is true ? SpawnButton ?? _services.Create<SpawnButtonViewModel>() : null;

	// The tools with settings or views of their own show them while enabled
	private void OnToolIsEnabledChanged(ToolBase tool, bool isEnabled)
	{
		switch (tool)
		{
			case ConstructTool:
				SetWidgetOpen<AssetSelectorViewModel>(isEnabled);
				break;

			case PropertyTool:
				SetWidgetOpen<PropertyEditorViewModel>(isEnabled);
				break;
		}
	}

	private void SetWidgetOpen<[DynamicallyAccessedMembers(Constructors)] TWidget>(bool isOpen)
		where TWidget : ViewModelBase, IWidget
	{
		if (isOpen)
			Widgets.Open<TWidget>();
		else
			Widgets.Close<TWidget>();
	}

	// Counts other players' messages that arrive while the chat is closed
	private void OnChatMessageAdded(ChatMessage message)
	{
		var isOwn = string.Equals(message.SenderId, GSessionManager.LocalPeer?.PlayerId, StringComparison.Ordinal);

		if (!_textChat.IsOpen && !message.IsNotice && !isOwn)
			_textChat.BadgeCount++;
	}

	private void OnTextChatPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(WidgetEntry.IsOpen) && _textChat.IsOpen)
			_textChat.BadgeCount = 0;
	}
}
