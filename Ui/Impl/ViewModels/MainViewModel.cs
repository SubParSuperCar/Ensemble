using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Attributes;

namespace Root.Ui.Impl.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class MainViewModel(IServiceProvider services) : ViewModelBase
{
	[ObservableProperty]
	[property: DisposeOldObservableValueOnChanging]
	public partial ViewModelBase? ViewModel { get; set; } = services.GetRequiredService<GameViewModel>();

	protected override void OnDispose() => ViewModel = null;
}
