using Avalonia;
using EnsembleRoot.Ui.Impl.Abstractions;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public class WebBrowserViewModel : ViewModelBase, IWidget
{
	public static WidgetDescriptor Descriptor { get; } = new(new Rect(0.2, 0.15, 0.6, 0.7))
	{
		Description = "Lets you browse the web without leaving Ensemble.",
		MinSize = new Size(480, 320)
	};
}
