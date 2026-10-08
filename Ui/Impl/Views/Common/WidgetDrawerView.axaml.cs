using Avalonia.Controls;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public sealed partial class WidgetDrawerView : UserControl, IViewFor<WidgetDrawerViewModel>
{
	public WidgetDrawerView()
	{
		InitializeComponent();
	}
}
