using Avalonia.Controls;
using Avalonia.Input;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class ToolBarView : UserControl, IViewFor<ToolBarViewModel>
{
	public ToolBarView()
	{
		InitializeComponent();
	}

	private void OnClearAllDoubleTapped(object? sender, TappedEventArgs e)
	{
		LocalPlot?.Instances.Clear();
		GToolManager.Destruct.Disable();
	}
}
