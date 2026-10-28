using Avalonia.Controls;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Controls;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class LogOutputView : UserControl, IViewFor<LogOutputViewModel>
{
	public LogOutputView()
	{
		InitializeComponent();

		StickyBottom.Apply(OutputScroll, Output);
		ScrollBarInset.Apply(OutputScroll, SourceLabel, Actions);
	}
}
