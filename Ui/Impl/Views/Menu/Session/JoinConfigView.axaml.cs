using Avalonia.Controls;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public partial class JoinConfigView : UserControl, IViewFor<JoinConfigViewModel>
{
	public JoinConfigView()
	{
		InitializeComponent();
	}
}
