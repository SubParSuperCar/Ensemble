using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Controls;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public partial class WidgetView : UserControl, IViewFor<WidgetViewModel>
{
	private DragKind _drag;
	private Rect _dragOrigin;
	private Point _dragStart;

	public WidgetView()
	{
		InitializeComponent();
		AddHandler(PointerPressedEvent, OnAnyPointerPressed, RoutingStrategies.Tunnel, true);
	}

	private WidgetViewModel? ViewModel => DataContext as WidgetViewModel;
	private WidgetPanel? Area => this.FindAncestorOfType<WidgetPanel>();

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		Frame.Classes.Add("shown");
	}

	private void OnAnyPointerPressed(object? sender, PointerPressedEventArgs e) => ViewModel?.Activate();
	private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e) => BeginDrag(DragKind.Move, e);
	private void OnResizeGripPointerPressed(object? sender, PointerPressedEventArgs e) => BeginDrag(DragKind.Resize, e);

	private void OnDragPointerMoved(object? sender, PointerEventArgs e)
	{
		if (_drag is DragKind.None || ViewModel is not { } viewModel || Area is not { } area)
			return;

		var delta = e.GetPosition(area) - _dragStart;

		if (_drag is DragKind.Move)
			viewModel.Move(_dragOrigin, delta, area.Bounds.Size);
		else
			viewModel.Resize(_dragOrigin, delta, area.Bounds.Size);
	}

	private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		_drag = DragKind.None;
		e.Pointer.Capture(null);
	}

	private void OnDragPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => _drag = DragKind.None;

	private void OnHeaderDoubleTapped(object? sender, TappedEventArgs e)
	{
		if (e.Source is not Visual source || source.FindAncestorOfType<Button>(true) is null)
			ViewModel?.ToggleMaximizedCommand.Execute(null);
	}

	private void BeginDrag(DragKind drag, PointerPressedEventArgs e)
	{
		if (
			ViewModel is not { } viewModel ||
			Area is not { } area ||
			!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
			return;

		_drag = drag;
		_dragOrigin = viewModel.Bounds;
		_dragStart = e.GetPosition(area);

		e.Pointer.Capture(e.Source as IInputElement);
		e.Handled = true;
	}

	private enum DragKind : byte
	{
		None,
		Move,
		Resize
	}
}
