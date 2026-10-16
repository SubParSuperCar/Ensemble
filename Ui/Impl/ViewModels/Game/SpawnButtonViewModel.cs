using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Sessions.Actions;
using EnsembleRoot.Ui.Impl.Abstractions;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed partial class SpawnButtonViewModel : ViewModelBase
{
	public SpawnButtonViewModel()
	{
		OnIsLocalPlotSpawnedChanged(IsLocalPlotSpawned);
		IsLocalPlotSpawnedChanged += OnIsLocalPlotSpawnedChanged;
	}

	[ObservableProperty] public partial string Label { get; private set; } = string.Empty;

	protected override void OnDispose() => IsLocalPlotSpawnedChanged -= OnIsLocalPlotSpawnedChanged;

	[RelayCommand]
	private static void Toggle() => new SetPlotSpawnedAction(IsLocalPlotSpawned is not true).Submit();

	private void OnIsLocalPlotSpawnedChanged(bool? isSpawned) => Label = isSpawned is true ? "Despawn" : "Spawn";
}
