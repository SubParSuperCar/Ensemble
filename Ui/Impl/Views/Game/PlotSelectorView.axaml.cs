using Avalonia.Controls;
using Avalonia.Input;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

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
