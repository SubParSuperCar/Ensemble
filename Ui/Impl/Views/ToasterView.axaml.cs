using Avalonia.Controls;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Controls;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class ToasterView : UserControl, IViewFor<ToasterViewModel>
{
	private const double ScrollBarGap = 4d;

	public ToasterView()
	{
		InitializeComponent();

		ScrollBarInset.ApplyGap(ToastScroll, Toasts, ScrollBarGap);
	}
}
