using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Root.Ui.Impl.Abstractions;

namespace Root.Ui.Impl.ViewModels;

public partial class DocFileViewModel : ViewModelBase
{
	private const string GhMainHeadPath = "https://raw.githubusercontent.com/SubParSuperCar/Ensemble/refs/heads/main/";

	public DocFileViewModel()
	{
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
}

public record DocFile(string Name, string Uri);
