using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Tooling.Tools;
using EnsembleRoot.Ui.Abstractions;
using Estragonia;
using Serilog;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class ToolBarViewModel : ViewModelBase
{
	private GdInstances? _instances;

	public ToolBarViewModel()
	{
		IsConstructToolEnabled = GToolManager.IsEnabled<ConstructTool>();
		IsDestructToolEnabled = GToolManager.IsEnabled<DestructTool>();
		IsPropertyToolEnabled = GToolManager.IsEnabled<PropertyTool>();
		IsPaintToolEnabled = GToolManager.IsEnabled<PaintTool>();
		GToolManager.ToolIsEnabledChanged += OnToolIsEnabledChanged;

		OnUndoRedoChanged();
		GToolManager.UndoRedo.Changed += OnUndoRedoChanged;

		OnLocalPlotChanged(LocalPlot);
		LocalPlotChanged += OnLocalPlotChanged;

		IsPlotOwnerChanged += OnPlotStateChanged;
		IsLocalPlotSpawnedChanged += OnPlotStateChanged;
	}

	public static IBrush ConstructBrush { get; } = new ImmutableSolidColorBrush(ConstructTool.Theme.ToAvaloniaColor());
	public static IBrush DestructBrush { get; } = new ImmutableSolidColorBrush(DestructTool.Theme.ToAvaloniaColor());
	public static IBrush PropertyBrush { get; } = new ImmutableSolidColorBrush(PropertyTool.Theme.ToAvaloniaColor());
	public static IBrush PaintBrush { get; } = new ImmutableSolidColorBrush(PaintTool.Theme.ToAvaloniaColor());

	[ObservableProperty] public partial bool IsConstructToolEnabled { get; set; }
	[ObservableProperty] public partial bool IsDestructToolEnabled { get; set; }
	[ObservableProperty] public partial bool IsPropertyToolEnabled { get; set; }
	[ObservableProperty] public partial bool IsPaintToolEnabled { get; set; }

	[ObservableProperty] public partial bool IsMutexEnforced { get; set; } = GToolManager.UseMutex;
	[ObservableProperty] public partial bool IsRestoreOnDespawnEnabled { get; set; } = GToolManager.RestoreOnDespawn;

	[ObservableProperty] public partial bool IsClearAllVisible { get; private set; }
	[ObservableProperty] public partial bool IsClearAllEnabled { get; private set; }

	[ObservableProperty] public partial bool IsPaintAllVisible { get; private set; }
	[ObservableProperty] public partial bool IsPaintAllEnabled { get; private set; }

	[ObservableProperty] public partial bool IsUndoEnabled { get; private set; }
	[ObservableProperty] public partial bool IsRedoEnabled { get; private set; }

	protected override void OnDispose()
	{
		GToolManager.ToolIsEnabledChanged -= OnToolIsEnabledChanged;
		GToolManager.UndoRedo.Changed -= OnUndoRedoChanged;

		LocalPlotChanged -= OnLocalPlotChanged;
		IsPlotOwnerChanged -= OnPlotStateChanged;
		IsLocalPlotSpawnedChanged -= OnPlotStateChanged;

		SetInstances(null);
	}

	public static void ClearAll()
	{
		GToolManager.Destruct.ClearAll();
		GToolManager.Destruct.Disable();
	}

	[RelayCommand]
	private static void ToggleConstructTool() => GToolManager.Toggle<ConstructTool>();

	[RelayCommand]
	private static void ToggleDestructTool() => GToolManager.Toggle<DestructTool>();

	[RelayCommand]
	private static void TogglePropertyTool() => GToolManager.Toggle<PropertyTool>();

	[RelayCommand]
	private static void TogglePaintTool() => GToolManager.Toggle<PaintTool>();

	[RelayCommand]
	private static void PaintAll() => GToolManager.Paint.PaintAll();

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
				UpdatePlotButtons();
				break;

			case PropertyTool:
				IsPropertyToolEnabled = isEnabled;
				break;

			case PaintTool:
				IsPaintToolEnabled = isEnabled;
				UpdatePlotButtons();
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
		UpdatePlotButtons();
	}

	private void OnPlotStateChanged(bool? _) => UpdatePlotButtons();
	private void OnInstanceCountChanged(GdInstance _) => UpdatePlotButtons();

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

	private void UpdatePlotButtons()
	{
		var isEditable = IsLocalPlotSpawned is false;
		var hasInstances = _instances is { Count: > 0 };

		IsClearAllVisible = IsDestructToolEnabled && IsPlotOwner is true && isEditable;
		IsClearAllEnabled = IsClearAllVisible && hasInstances;

		IsPaintAllVisible = IsPaintToolEnabled && isEditable;
		IsPaintAllEnabled = IsPaintAllVisible && hasInstances;
	}
}
