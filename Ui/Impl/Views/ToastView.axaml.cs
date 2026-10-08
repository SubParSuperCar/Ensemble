using Avalonia.Controls;
using Avalonia.Interactivity;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public sealed partial class ToastView : UserControl, IViewFor<ToastViewModel>
{
	public ToastView()
	{
		InitializeComponent();
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		Frame.Classes.Add("shown");
	}
}
