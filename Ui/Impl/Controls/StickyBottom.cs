using Avalonia.Controls;

namespace EnsembleRoot.Ui.Controls;

/// <summary>
///     Keeps a scroll viewer at its bottom while text arrives or the viewer resizes, until the user scrolls more than a
///     line up.
/// </summary>
/// <remarks>Starts stuck, so the first text lands at the bottom however late layout settles.</remarks>
public static class StickyBottom
{
	public static void Apply(ScrollViewer scrollViewer, TextBlock text)
	{
		var isStuck = true;

		scrollViewer.ScrollChanged += (_, e) =>
		{
			if (e.ExtentDelta.Y > 0 || e.ViewportDelta.Y is not 0)
			{
				if (isStuck)
					scrollViewer.ScrollToEnd();

				return;
			}

			var distanceToBottom = scrollViewer.Extent.Height - scrollViewer.Offset.Y - scrollViewer.Viewport.Height;
			isStuck = distanceToBottom <= text.FontSize;
		};
	}
}
