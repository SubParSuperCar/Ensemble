using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Services;

namespace Root.Ui.Impl.ViewModels;

public partial class DocFileViewModel : ViewModelBase
{
	private const string GhMainHeadPath = "https://raw.githubusercontent.com/SubParSuperCar/Ensemble/refs/heads/main/";
	private readonly NavigatorService _navigator;

	public DocFileViewModel(NavigatorService navigator)
	{
		_navigator = navigator;
		SelectedFile = Files[0];
	}

	public ObservableCollection<DocFile> Files { get; } =
	[
		new("README.md", GhMainHeadPath + ".github/README.md"),
		new("CONTRIBUTING.md", GhMainHeadPath + ".github/CONTRIBUTING.md"),
		new("LICENSE.md", GhMainHeadPath + "LICENSE.md"),
		new("LICENSE-ASSETS.txt", GhMainHeadPath + ".github/LICENSE-ASSETS.txt"),
		new("LICENSE-CODE.txt", GhMainHeadPath + ".github/LICENSE-CODE.txt")
	];

	[ObservableProperty] public partial DocFile SelectedFile { get; set; }

	[RelayCommand]
	private void GoBack() => _navigator.GoBack();
}

public record DocFile(string Name, string Uri);
