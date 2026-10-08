using Avalonia;
using EnsembleRoot.Ui.Impl.Abstractions;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed class WebBrowserViewModel : ViewModelBase, IWidget
{
	public static WidgetDescriptor Descriptor { get; } = new(WidgetDescriptor.Cells(3, 2, 10, 12))
	{
		Description = "Lets you browse the web without leaving Ensemble.",
		MinSize = new Size(480, 320)
	};
}
