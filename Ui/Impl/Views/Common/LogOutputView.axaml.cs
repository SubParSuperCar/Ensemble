using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Controls;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class LogOutputView : UserControl, IViewFor<LogOutputViewModel>
{
	private const string EditorScrollViewerName = "PART_ScrollViewer";
	private const string SelectionClass = "selection";

	public LogOutputView()
	{
		InitializeComponent();
		Output.TextArea.SelectionChanged += OnSelectionChanged;
	}

	private void OnOutputTemplateApplied(object? sender, TemplateAppliedEventArgs e)
	{
		if (e.NameScope.Find<ScrollViewer>(EditorScrollViewerName) is not { } scrollViewer)
			return;

		StickyBottom.Apply(scrollViewer, Output);
		ScrollBarInset.Apply(scrollViewer, SourceLabel, Actions);
	}

	private void OnSelectionChanged(object? sender, EventArgs e) =>
		Output.TextArea.Classes.Set(SelectionClass, !Output.TextArea.Selection.IsEmpty);
}
