using Avalonia.Controls;
using Avalonia.Input;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;
using Serilog;

namespace Root.Ui.Impl.Views;

public partial class ToolBarView : UserControl, IViewFor<ToolBarViewModel>
{
	public ToolBarView()
	{
		InitializeComponent();
	}

	private void OnClearAllDoubleTapped(object? sender, TappedEventArgs e)
	{
		if (LocalPlot?.Instances is { } instances)
		{
			Log.Debug("Clearing {Count} local plot instance(s)...", instances.Count);
			instances.Clear();
		}

		GToolManager.Destruct.Disable();
	}
}
