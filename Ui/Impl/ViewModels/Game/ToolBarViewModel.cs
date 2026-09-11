using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Root.GdCore.Assets;
using Root.GdCore.Plots;
using Root.Tooling.Tools;
using Root.Ui.Impl.Abstractions;
using Serilog;

namespace Root.Ui.Impl.ViewModels;

public partial class ToolBarViewModel : ViewModelBase
{
	private GdInstances? _instances;

	public ToolBarViewModel()
	{
		GToolManager.Construct.IsEnabledChanged += OnConstructToolIsEnabledChanged;
		GToolManager.Destruct.IsEnabledChanged += OnDestructToolIsEnabledChanged;

		LocalPlotChanged += OnLocalPlotChanged;
		IsPlotOwnerChanged += OnClearAllConditionChanged;
		IsLocalPlotSpawnedChanged += OnClearAllConditionChanged;

		OnLocalPlotChanged(LocalPlot);
	}

	[ObservableProperty] public partial bool IsConstructToolEnabled { get; set; }
	[ObservableProperty] public partial bool IsDestructToolEnabled { get; set; }

	[ObservableProperty] public partial bool IsMutexEnforced { get; set; } = GToolManager.UseMutex;

	[ObservableProperty] public partial bool IsClearAllVisible { get; set; }
	[ObservableProperty] public partial bool IsClearAllEnabled { get; set; }

	protected override void OnDispose()
	{
		GToolManager.Construct.IsEnabledChanged -= OnConstructToolIsEnabledChanged;
		GToolManager.Destruct.IsEnabledChanged -= OnDestructToolIsEnabledChanged;

		LocalPlotChanged -= OnLocalPlotChanged;
		IsPlotOwnerChanged -= OnClearAllConditionChanged;
		IsLocalPlotSpawnedChanged -= OnClearAllConditionChanged;

		SetInstances(null);
	}

	[RelayCommand]
	private static void ToggleConstructTool() => GToolManager.Construct.Toggle();

	[RelayCommand]
	private static void ToggleDestructTool() => GToolManager.Destruct.Toggle();

	[RelayCommand]
	private void ToggleMutexEnforced()
	{
		GToolManager.UseMutex = !GToolManager.UseMutex;
		IsMutexEnforced = GToolManager.UseMutex;
	}

	private void OnConstructToolIsEnabledChanged(bool isEnabled)
	{
		Log.Verbose("{Tool}.{Member} set to: {Value}", nameof(ConstructTool), nameof(ToolBase.IsEnabled), isEnabled);
		IsConstructToolEnabled = isEnabled;
	}

	private void OnDestructToolIsEnabledChanged(bool isEnabled)
	{
		Log.Verbose("{Tool}.{Member} set to: {Value}", nameof(DestructTool), nameof(ToolBase.IsEnabled), isEnabled);
		IsDestructToolEnabled = isEnabled;
		UpdateClearAll();
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
