using Avalonia.Controls;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;

namespace Root.Ui.Impl.Views;

// ReSharper disable once UnusedType.Global
public partial class WebBrowserView : UserControl, IViewFor<WebBrowserViewModel>
{
	public WebBrowserView()
	{
		InitializeComponent();
	}
}
