using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace EnsembleRoot.Ui.Controls;

/// <summary>
///     Keeps overlays (e.g., buttons over scrollable content) clear of a scroll viewer's vertical scroll bar by adding
///     its width to their right margins while it's visible.
/// </summary>
/// <remarks>
///     Measures the scroll bar itself, since logical scrollers (e.g., AvaloniaEdit's) report viewports narrower than
///     their scroll viewers by more than the scroll bar.
/// </remarks>
public static class ScrollBarInset
{
	private const string VerticalScrollBarName = "PART_VerticalScrollBar";

	/// <remarks>Call before <paramref name="scrollViewer" />'s template is applied, e.g., in a constructor.</remarks>
	public static void Apply(ScrollViewer scrollViewer, params Control[] overlays)
	{
		var margins = Array.ConvertAll(overlays, static overlay => overlay.Margin);

		scrollViewer.TemplateApplied += (_, e) =>
		{
			if (e.NameScope.Find<ScrollBar>(VerticalScrollBarName) is not { } scrollBar)
				return;

			scrollBar.PropertyChanged += (_, change) =>
			{
				if (change.Property == Visual.BoundsProperty || change.Property == Visual.IsVisibleProperty)
					Update(scrollBar);
			};

			Update(scrollBar);
		};

		return;

		void Update(ScrollBar scrollBar)
		{
			var inset = scrollBar.IsVisible ? scrollBar.Bounds.Width : 0;

			foreach (var (overlay, margin) in overlays.Zip(margins))
				overlay.Margin = new Thickness(margin.Left, margin.Top, margin.Right + inset, margin.Bottom);
		}
	}
}
