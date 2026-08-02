using Avalonia.Controls;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

public partial class LoadingIndicatorView : UserControl, IViewFor<LoadingIndicatorViewModel>
{
	public LoadingIndicatorView()
	{
		InitializeComponent();
	}
}
