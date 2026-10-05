using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class WidgetDrawerViewModel(WidgetManagerService widgets) : ViewModelBase
{
	public ObservableCollection<WidgetEntry> Entries { get; } = widgets.Entries;

	[ObservableProperty] public partial bool IsExpanded { get; set; } = true;
}
