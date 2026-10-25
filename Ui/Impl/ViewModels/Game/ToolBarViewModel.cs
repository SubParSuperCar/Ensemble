using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Sessions.Actions;
using EnsembleRoot.Tooling.Tools;
using EnsembleRoot.Ui.Impl.Abstractions;
using Estragonia;
using Serilog;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed partial class ToolBarViewModel : ViewModelBase
{
	private GdInstances? _instances;

	public ToolBarViewModel()
	{
		IsConstructToolEnabled = GToolManager.IsEnabled<ConstructTool>();
		IsDestructToolEnabled = GToolManager.IsEnabled<DestructTool>();
		IsEditToolEnabled = GToolManager.IsEnabled<EditTool>();
		GToolManager.ToolIsEnabledChanged += OnToolIsEnabledChanged;

		OnUndoRedoChanged();
		GToolManager.UndoRedo.Changed += OnUndoRedoChanged;

		OnLocalPlotChanged(LocalPlot);
		LocalPlotChanged += OnLocalPlotChanged;

		IsPlotOwnerChanged += OnClearAllConditionChanged;
		IsLocalPlotSpawnedChanged += OnClearAllConditionChanged;
	}

	public static IBrush ConstructBrush { get; } = new ImmutableSolidColorBrush(ConstructTool.Theme.ToAvaloniaColor());
	public static IBrush DestructBrush { get; } = new ImmutableSolidColorBrush(DestructTool.Theme.ToAvaloniaColor());
	public static IBrush EditBrush { get; } = new ImmutableSolidColorBrush(EditTool.Theme.ToAvaloniaColor());

	[ObservableProperty] public partial bool IsConstructToolEnabled { get; set; }
	[ObservableProperty] public partial bool IsDestructToolEnabled { get; set; }
	[ObservableProperty] public partial bool IsEditToolEnabled { get; set; }

	[ObservableProperty] public partial bool IsMutexEnforced { get; set; } = GToolManager.UseMutex;
	[ObservableProperty] public partial bool IsRestoreOnDespawnEnabled { get; set; } = GToolManager.RestoreOnDespawn;

	[ObservableProperty] public partial bool IsClearAllVisible { get; private set; }
	[ObservableProperty] public partial bool IsClearAllEnabled { get; private set; }

	[ObservableProperty] public partial bool IsUndoEnabled { get; private set; }
	[ObservableProperty] public partial bool IsRedoEnabled { get; private set; }

	protected override void OnDispose()
	{
		GToolManager.ToolIsEnabledChanged -= OnToolIsEnabledChanged;
		GToolManager.UndoRedo.Changed -= OnUndoRedoChanged;

		LocalPlotChanged -= OnLocalPlotChanged;
		IsPlotOwnerChanged -= OnClearAllConditionChanged;
		IsLocalPlotSpawnedChanged -= OnClearAllConditionChanged;

		SetInstances(null);
	}

	public static void ClearAll()
	{
		Log.Debug("Clearing {Count} local plot instance(s)...", LocalPlot?.Instances.Count);
		new ClearInstancesAction().Submit();

		GToolManager.Destruct.Disable();
	}

	[RelayCommand]
	private static void ToggleConstructTool() => GToolManager.Toggle<ConstructTool>();

	[RelayCommand]
	private static void ToggleDestructTool() => GToolManager.Toggle<DestructTool>();

	[RelayCommand]
	private static void ToggleEditTool() => GToolManager.Toggle<EditTool>();

	[RelayCommand]
	private static void Undo() => GToolManager.UndoRedo.Undo();

	[RelayCommand]
	private static void Redo() => GToolManager.UndoRedo.Redo();

	partial void OnIsMutexEnforcedChanged(bool value) => GToolManager.UseMutex = value;
	partial void OnIsRestoreOnDespawnEnabledChanged(bool value) => GToolManager.RestoreOnDespawn = value;

	private void OnToolIsEnabledChanged(ToolBase tool, bool isEnabled)
	{
		Log.Verbose("{Tool}.{Member} set to: {Value}", tool.Name, nameof(ToolBase.IsEnabled), isEnabled);

		switch (tool)
		{
			case ConstructTool:
				IsConstructToolEnabled = isEnabled;
				break;

			case DestructTool:
				IsDestructToolEnabled = isEnabled;
				UpdateClearAll();
				break;

			case EditTool:
				IsEditToolEnabled = isEnabled;
				break;
		}
	}

	private void OnUndoRedoChanged()
	{
		IsUndoEnabled = GToolManager.UndoRedo.CanUndo;
		IsRedoEnabled = GToolManager.UndoRedo.CanRedo;
	}

	private void OnLocalPlotChanged(GdPlot? plot)
	{
		SetInstances(plot?.Instances);
		UpdateClearAll();
	}

	private void OnClearAllConditionChanged(bool? _) => UpdateClearAll();
	private void OnInstanceCountChanged(GdInstance _) => UpdateClearAll();

	private void SetInstances(GdInstances? instances)
	{
		if (ReferenceEquals(_instances, instances))
			return;

		if (_instances is not null)
		{
			_instances.Added -= OnInstanceCountChanged;
			_instances.Removed -= OnInstanceCountChanged;
		}

		_instances = instances;

		if (instances is null)
			return;

		instances.Added += OnInstanceCountChanged;
		instances.Removed += OnInstanceCountChanged;
	}

	private void UpdateClearAll()
	{
		var isVisible = IsDestructToolEnabled && IsPlotOwner is true && IsLocalPlotSpawned is not true;

		IsClearAllVisible = isVisible;
		IsClearAllEnabled = isVisible && _instances is { Count: > 0 };
	}
}
