using CommunityToolkit.Mvvm.Input;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Services;

namespace Root.Ui.Impl.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class ReadMeViewModel(NavigatorService navigator) : ViewModelBase
{
	[RelayCommand]
	private void GoBack() => navigator.GoBack();
}
