using Avalonia.Controls;
using Avalonia.Input;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class AssetSelectorView : UserControl, IViewFor<AssetSelectorViewModel>
{
	public AssetSelectorView()
	{
		InitializeComponent();
	}

	private void OnFilterBoxGotFocus(object? sender, FocusChangedEventArgs e)
	{
		if (DataContext is AssetSelectorViewModel viewModel)
			viewModel.FilterQuery = string.Empty;
	}

	private void OnTreeViewContainerPrepared(object? sender, ContainerPreparedEventArgs e)
	{
		if (sender is not ItemsControl owner || e.Container is not TreeViewItem item)
			return;

		item.Classes.Set("folder", owner.ItemFromContainer(item) is FolderNode);

		item.ContainerPrepared -= OnTreeViewContainerPrepared;
		item.ContainerPrepared += OnTreeViewContainerPrepared;
	}
}
