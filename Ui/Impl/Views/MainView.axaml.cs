using Avalonia.Controls;
using Avalonia.Interactivity;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class MainView : UserControl
{
	public MainView()
	{
		InitializeComponent();
		DataContext = new MainViewModel();
	}

	protected override void OnUnloaded(RoutedEventArgs e)
	{
		base.OnUnloaded(e);

		var vm = DataContext as IDisposable;
		DataContext = null;
		vm?.Dispose();
	}
}
