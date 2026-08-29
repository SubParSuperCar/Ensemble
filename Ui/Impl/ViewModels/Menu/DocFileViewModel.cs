using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Services;

namespace Root.Ui.Impl.ViewModels;

public partial class DocFileViewModel : ViewModelBase
{
	private const string MainHeadPath = "https://raw.githubusercontent.com/SubParSuperCar/Ensemble/refs/heads/main/";
	private readonly NavigatorService _navigator;

	public DocFileViewModel(NavigatorService navigator)
	{
		_navigator = navigator;
		SelectedFile = Files[0];
	}

	public ObservableCollection<File> Files { get; } =
	[
		new("README.md", MainHeadPath + ".github/README.md"),
		new("CONTRIBUTING.md", MainHeadPath + ".github/CONTRIBUTING.md"),
		new("LICENSE.md", MainHeadPath + "LICENSE.md"),
		new("LICENSE-ASSETS.txt", MainHeadPath + ".github/LICENSE-ASSETS.txt"),
		new("LICENSE-CODE.txt", MainHeadPath + ".github/LICENSE-CODE.txt")
	];

	[ObservableProperty] public partial File SelectedFile { get; set; }

	[RelayCommand]
	private void GoBack() => _navigator.GoBack();
}

public record File(string Name, string Uri);
