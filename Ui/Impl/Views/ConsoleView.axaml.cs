using Avalonia.Controls;
using Avalonia.Threading;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public sealed partial class ConsoleView : UserControl, IViewFor<ConsoleViewModel>
{
	private bool _shouldScrollToBottom;

	public ConsoleView()
	{
		InitializeComponent();

		OutputScroll.ScrollChanged += OnOutputScrollChanged;
		Dispatcher.UIThread.Post(OutputScroll.ScrollToEnd, DispatcherPriority.Loaded);
	}

	private void OnOutputScrollChanged(object? sender, ScrollChangedEventArgs e)
	{
		if (e.ExtentDelta.Y > 0)
		{
			if (_shouldScrollToBottom)
				OutputScroll.ScrollToEnd();

			return;
		}

		var distanceToBottom = OutputScroll.Extent.Height - OutputScroll.Offset.Y - OutputScroll.Viewport.Height;
		_shouldScrollToBottom = distanceToBottom <= Output.FontSize;
	}
}
