using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Root.Tooling;
using Root.Ui.Impl.Abstractions;
using Serilog;

namespace Root.Ui.Impl.ViewModels;

public partial class ToolBarViewModel : ViewModelBase
{
	public ToolBarViewModel()
	{
		GToolManager.Construct.IsEnabledChanged += OnConstructToolIsEnabledChanged;
		GToolManager.Destruct.IsEnabledChanged += OnDestructToolIsEnabledChanged;
	}

	[ObservableProperty] public partial bool IsConstructToolEnabled { get; set; }
	[ObservableProperty] public partial bool IsDestructToolEnabled { get; set; }

	[ObservableProperty] public partial bool IsMutexEnforced { get; set; } = GToolManager.UseMutex;

	protected override void OnDispose()
	{
		GToolManager.Construct.IsEnabledChanged -= OnConstructToolIsEnabledChanged;
		GToolManager.Destruct.IsEnabledChanged -= OnDestructToolIsEnabledChanged;
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
	}
}
