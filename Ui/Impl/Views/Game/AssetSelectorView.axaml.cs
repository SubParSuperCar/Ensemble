using Avalonia.Controls;
using Avalonia.Input;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

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
