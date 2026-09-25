using Avalonia.Controls;
using Avalonia.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public partial class AssetSelectorView : UserControl, IViewFor<AssetSelectorViewModel>
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
}
