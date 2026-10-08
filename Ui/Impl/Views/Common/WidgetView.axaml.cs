using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Controls;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public sealed partial class WidgetView : UserControl, IViewFor<WidgetViewModel>
{
	private const double HiddenScale = 3 / 4d;
	private const double HiddenTilt = -90 * (1 / 4d);
	private const double TiltDepth = 1000d;

	// Asymmetric by design
	private static readonly Easing ShownEasing = new QuadraticEaseOut();
	private static readonly Easing HiddenEasing = new LinearEasing();

	private readonly ScaleTransform _scale = new(HiddenScale, HiddenScale);
	private readonly Rotate3DTransform _tilt = new() { AngleX = HiddenTilt, Depth = TiltDepth };
	private readonly List<DoubleTransition> _transitions = [];
	private DragKind _drag;
	private Rect _dragOrigin;
	private Point _dragStart;

	private bool? _isShown;

	public WidgetView()
	{
		InitializeComponent();
		InitializeTransform();
		AddHandler(PointerPressedEvent, OnAnyPointerPressed, RoutingStrategies.Tunnel, true);
	}

	private WidgetViewModel? ViewModel => DataContext as WidgetViewModel;
	private WidgetPanel? Area => this.FindAncestorOfType<WidgetPanel>();

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		Frame.Classes.Add("shown");
	}

	// Transform strings have no 3D rotations
	private void InitializeTransform()
	{
		_transitions.AddRange(
		[
			Ease(_scale, ScaleTransform.ScaleXProperty),
			Ease(_scale, ScaleTransform.ScaleYProperty),
			Ease(_tilt, Rotate3DTransform.AngleXProperty)
		]);

		Frame.RenderTransform = new TransformGroup { Children = [_scale, _tilt] };
		Frame.Classes.CollectionChanged += (_, _) => UpdateTransform();

		return;

		DoubleTransition Ease(Transform target, AvaloniaProperty property)
		{
			var transition = new DoubleTransition { Property = property, Duration = AnimationDuration };

			target.Transitions ??= [];
			target.Transitions.Add(transition);

			return transition;
		}
	}

	private void UpdateTransform()
	{
		var isShown = Frame.Classes.Contains("shown") && !Frame.Classes.Contains("hidden");

		if (isShown == _isShown)
			return;

		_isShown = isShown;

		foreach (var transition in _transitions)
			transition.Easing = isShown ? ShownEasing : HiddenEasing;

		_scale.ScaleX = _scale.ScaleY = isShown ? 1 : HiddenScale;
		_tilt.AngleX = isShown ? 0 : HiddenTilt;
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
