using Avalonia.Controls;
using Avalonia.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;
using Serilog;

namespace EnsembleRoot.Ui.Impl.Views;

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
