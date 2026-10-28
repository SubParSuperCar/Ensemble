using Avalonia.Controls;
using Avalonia.Interactivity;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class ToastView : UserControl, IViewFor<ToastViewModel>
{
	public ToastView()
	{
		InitializeComponent();
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		Frame.Classes.Add(App.ShownClass);
	}
}
