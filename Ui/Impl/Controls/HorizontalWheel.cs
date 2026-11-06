using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace EnsembleRoot.Ui.Controls;

/// <summary>
///     Scrolls a scroll viewer that only scrolls sideways with the vertical wheel too, as Avalonia only scrolls it
///     with a horizontal wheel or a trackpad.
/// </summary>
public static class HorizontalWheel
{
	// Avalonia's own step per wheel notch
	private const double Step = 50d;

	public static void Apply(ScrollViewer scrollViewer) =>
		scrollViewer.AddHandler(
			InputElement.PointerWheelChangedEvent,
			(_, e) =>
			{
				if (e.Delta.Y is 0 || e.Delta.X is not 0)
					return;

				scrollViewer.Offset = scrollViewer.Offset.WithX(scrollViewer.Offset.X - e.Delta.Y * Step);
				e.Handled = true;
			},
			RoutingStrategies.Tunnel);
}
