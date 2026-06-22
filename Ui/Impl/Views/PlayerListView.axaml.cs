using Avalonia.Controls;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

// ReSharper disable once UnusedType.Global
public partial class PlayerListView : UserControl
{
	public PlayerListView()
	{
		InitializeComponent();
		DataContext = new PlayerListViewModel();
	}
}
