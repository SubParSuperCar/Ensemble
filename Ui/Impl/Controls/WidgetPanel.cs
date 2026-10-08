using Avalonia;
using Avalonia.Controls;

namespace EnsembleRoot.Ui.Impl.Controls;

/// <summary>
///     Arranges children by bounds relative to its size, from 0 to 1, keeping each whole and at least its minimum size.
/// </summary>
public sealed class WidgetPanel : Panel
{
	public static readonly AttachedProperty<Rect> RelativeBoundsProperty =
		AvaloniaProperty.RegisterAttached<WidgetPanel, Control, Rect>("RelativeBounds");

	public static readonly AttachedProperty<Size> MinSizeProperty =
		AvaloniaProperty.RegisterAttached<WidgetPanel, Control, Size>("MinSize");

	static WidgetPanel()
	{
		AffectsParentMeasure<WidgetPanel>(RelativeBoundsProperty, MinSizeProperty);
		AffectsParentArrange<WidgetPanel>(RelativeBoundsProperty, MinSizeProperty);
	}

	public static Rect GetRelativeBounds(Control control) => control.GetValue(RelativeBoundsProperty);

	public static void SetRelativeBounds(Control control, Rect value) =>
		control.SetValue(RelativeBoundsProperty, value);

	public static Size GetMinSize(Control control) => control.GetValue(MinSizeProperty);
	public static void SetMinSize(Control control, Size value) => control.SetValue(MinSizeProperty, value);

	protected override Size MeasureOverride(Size availableSize)
	{
		var size = new Size(
			double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
			double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);

		foreach (var child in Children)
			child.Measure(Resolve(child, size).Size);

		return size;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		foreach (var child in Children)
			child.Arrange(Resolve(child, finalSize));

		return finalSize;
	}

	private static Rect Resolve(Control child, Size size)
	{
		var bounds = GetRelativeBounds(child);
		var minSize = GetMinSize(child);

		var width = Math.Min(Math.Max(bounds.Width * size.Width, minSize.Width), size.Width);
		var height = Math.Min(Math.Max(bounds.Height * size.Height, minSize.Height), size.Height);

		return new Rect(
			Math.Clamp(bounds.X * size.Width, 0, size.Width - width),
			Math.Clamp(bounds.Y * size.Height, 0, size.Height - height),
			width,
			height);
	}
}
