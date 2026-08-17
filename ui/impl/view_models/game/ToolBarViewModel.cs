using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Root.Tool;
using Root.Ui.Impl.Abstractions;
using Serilog;

namespace Root.Ui.Impl.ViewModels;

public partial class ToolBarViewModel : ViewModelBase
{
	public ToolBarViewModel()
	{
		GToolManager.Place.IsEnabledChanged += OnPlaceToolIsEnabledChanged;
		GToolManager.Delete.IsEnabledChanged += OnDeleteToolIsEnabledChanged;
	}

	[ObservableProperty] public partial bool IsPlaceToolEnabled { get; set; }
	[ObservableProperty] public partial bool IsDeleteToolEnabled { get; set; }

	[ObservableProperty] public partial bool IsMutexEnforced { get; set; } = GToolManager.UseMutex;

	protected override void OnDispose()
	{
		GToolManager.Place.IsEnabledChanged -= OnPlaceToolIsEnabledChanged;
		GToolManager.Delete.IsEnabledChanged -= OnDeleteToolIsEnabledChanged;
	}

	[RelayCommand]
	private static void TogglePlaceTool() => GToolManager.Place.Toggle();

	[RelayCommand]
	private static void ToggleDeleteTool() => GToolManager.Delete.Toggle();

	[RelayCommand]
	private void ToggleMutexEnforced()
	{
		GToolManager.UseMutex = !GToolManager.UseMutex;
		IsMutexEnforced = GToolManager.UseMutex;
	}

	private void OnPlaceToolIsEnabledChanged(bool isEnabled)
	{
		Log.Verbose("{Tool}.{Member} set to: {Value}", nameof(PlaceTool), nameof(ToolBase.IsEnabled), isEnabled);
		IsPlaceToolEnabled = isEnabled;
	}

	private void OnDeleteToolIsEnabledChanged(bool isEnabled)
	{
		Log.Verbose("{Tool}.{Member} set to: {Value}", nameof(DeleteTool), nameof(ToolBase.IsEnabled), isEnabled);
		IsDeleteToolEnabled = isEnabled;
	}
}
