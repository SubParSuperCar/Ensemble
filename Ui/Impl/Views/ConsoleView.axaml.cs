using Avalonia.Controls;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public sealed partial class ConsoleView : UserControl, IViewFor<ConsoleViewModel>
{
	private readonly double[] _minWidths;

	public ConsoleView()
	{
		InitializeComponent();

		_minWidths = [.. Panes.ColumnDefinitions.Select(static column => column.MinWidth)];
	}

	// When both panes can't fit, they shrink proportionally instead of the editor overflowing
	private void OnPanesSizeChanged(object? sender, SizeChangedEventArgs e)
	{
		var scale = Math.Min((e.NewSize.Width - Splitter.Width) / _minWidths.Sum(), 1d);

		for (var i = 0; i < _minWidths.Length; i++)
			Panes.ColumnDefinitions[i].MinWidth = _minWidths[i] * scale;
	}
}
