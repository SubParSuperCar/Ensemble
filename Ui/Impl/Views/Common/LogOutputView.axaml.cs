using Avalonia.Controls;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Controls;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class LogOutputView : UserControl, IViewFor<LogOutputViewModel>
{
	// Starts stuck, so the first output lands at the bottom however late layout settles
	private bool _shouldScrollToBottom = true;

	public LogOutputView()
	{
		InitializeComponent();

		OutputScroll.ScrollChanged += OnOutputScrollChanged;
		ScrollBarInset.Apply(OutputScroll, SourceLabel, Actions);
	}

	// Sticks to the bottom while output arrives or the view resizes, until the user scrolls up
	private void OnOutputScrollChanged(object? sender, ScrollChangedEventArgs e)
	{
		if (e.ExtentDelta.Y > 0 || e.ViewportDelta.Y is not 0)
		{
			if (_shouldScrollToBottom)
				OutputScroll.ScrollToEnd();

			return;
		}

		var distanceToBottom = OutputScroll.Extent.Height - OutputScroll.Offset.Y - OutputScroll.Viewport.Height;
		_shouldScrollToBottom = distanceToBottom <= Output.FontSize;
	}
}
