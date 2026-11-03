using Avalonia.Controls;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Controls;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class ToastListView : UserControl, IViewFor<ToastListViewModel>
{
	private const double ScrollBarGap = 4d;

	public ToastListView()
	{
		InitializeComponent();

		ScrollBarInset.ApplyGap(ToastScroll, Toasts, ScrollBarGap);
	}
}
