using Avalonia.Controls;
using Avalonia.Input;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.SessionManager.Actions;
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
		Log.Debug("Clearing {Count} local plot instance(s)...", LocalPlot?.Instances.Count);
		new ClearInstancesAction().Submit();

		GToolManager.Destruct.Disable();
	}
}
