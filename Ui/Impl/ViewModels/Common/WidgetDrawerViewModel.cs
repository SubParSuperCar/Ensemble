using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Services;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class WidgetDrawerViewModel(WidgetManagerService widgets) : ViewModelBase
{
	public ObservableCollection<WidgetEntry> Entries { get; } = widgets.Entries;

	[ObservableProperty] public partial bool IsExpanded { get; set; } = true;
}
