using Avalonia.Controls;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public sealed partial class HostConfigView : UserControl, IViewFor<HostConfigViewModel>
{
	public HostConfigView()
	{
		InitializeComponent();
	}
}
