using Avalonia.Controls;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

// ReSharper disable once UnusedType.Global
public partial class PlayerListView : UserControl, IViewFor<PlayerListViewModel>
{
	public PlayerListView()
	{
		InitializeComponent();
	}
}
