using Avalonia.Controls;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class AssetSelectorView : UserControl, IViewFor<AssetSelectorViewModel>
{
	public AssetSelectorView()
	{
		InitializeComponent();
	}
}
