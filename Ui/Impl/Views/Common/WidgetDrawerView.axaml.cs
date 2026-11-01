using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class WidgetDrawerView : UserControl, IViewFor<WidgetDrawerViewModel>
{
	private readonly TranslateTransform _slide = new()
	{
		Transitions = [new DoubleTransition { Property = TranslateTransform.XProperty, Duration = AnimationDuration }]
	};

	public WidgetDrawerView()
	{
		InitializeComponent();
		Drawer.RenderTransform = _slide;
	}

	private void OnMenuSizeChanged(object? sender, SizeChangedEventArgs e) => UpdateSlide();
	private void OnHandleIsCheckedChanged(object? sender, RoutedEventArgs e) => UpdateSlide();

	private void UpdateSlide() => _slide.X = Handle.IsChecked is true ? 0 : -Menu.Bounds.Width;
}
