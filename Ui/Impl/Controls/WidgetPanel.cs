using Avalonia;
using Avalonia.Controls;

namespace EnsembleRoot.Ui.Controls;

/// <summary>
///     Arranges children by bounds relative to its size, from 0 to 1, keeping each whole and at least its minimum size.
/// </summary>
public sealed partial class WidgetPanel : Panel
{
	static WidgetPanel()
	{
		AffectsParentMeasure<WidgetPanel>(RelativeBoundsProperty, MinSizeProperty);
		AffectsParentArrange<WidgetPanel>(RelativeBoundsProperty, MinSizeProperty);
	}

	[GeneratedAttachedProperty]
	public static partial Rect GetRelativeBounds(Control control);

	[GeneratedAttachedProperty]
	public static partial Size GetMinSize(Control control);

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

		// Not Math.Clamp, which throws when the minimum exceeds the available size
		var width = Math.Min(Math.Max(bounds.Width * size.Width, minSize.Width), size.Width);
		var height = Math.Min(Math.Max(bounds.Height * size.Height, minSize.Height), size.Height);

		var x = Math.Clamp(bounds.X * size.Width, 0, size.Width - width);
		var y = Math.Clamp(bounds.Y * size.Height, 0, size.Height - height);

		return new Rect(x, y, width, height);
	}
}
