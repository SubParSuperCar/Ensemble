using Avalonia.Controls;
using Avalonia.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public sealed partial class PlotSelectorView : UserControl, IViewFor<PlotSelectorViewModel>
{
	public PlotSelectorView()
	{
		InitializeComponent();
	}

	private void OnItemPointerEntered(object? sender, PointerEventArgs e) =>
		PlotSelectorViewModel.SetHoveredPlot((sender as Control)?.DataContext as PlotItem);

	private void OnItemPointerExited(object? sender, PointerEventArgs e) => PlotSelectorViewModel.SetHoveredPlot(null);
}
