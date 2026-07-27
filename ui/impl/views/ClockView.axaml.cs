using Avalonia.Controls;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

// ReSharper disable once UnusedType.Global
public partial class ClockView : UserControl, IViewFor<ClockViewModel>
{
	public ClockView()
	{
		InitializeComponent();
	}
}
