using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

	[ObservableProperty] public partial bool IsMutexEnabled { get; set; } = GToolManager.UseMutex;

	protected override void OnDispose()
	{
		GToolManager.Place.IsEnabledChanged -= OnPlaceToolIsEnabledChanged;
		GToolManager.Delete.IsEnabledChanged -= OnDeleteToolIsEnabledChanged;
	}

	[RelayCommand]
	private static void TogglePlaceToolEnabled() => GToolManager.Place.Toggle();

	[RelayCommand]
	private static void ToggleDeleteToolEnabled() => GToolManager.Delete.Toggle();

	[RelayCommand]
	private void ToggleMutexEnabled()
	{
		GToolManager.UseMutex = !GToolManager.UseMutex;
		IsMutexEnabled = GToolManager.UseMutex;
	}

	private void OnPlaceToolIsEnabledChanged(bool isEnabled)
	{
		Log.Verbose("Place tool toggled");
		IsPlaceToolEnabled = isEnabled;
	}

	private void OnDeleteToolIsEnabledChanged(bool isEnabled)
	{
		Log.Verbose("Delete tool toggled");
		IsDeleteToolEnabled = isEnabled;
	}
}
