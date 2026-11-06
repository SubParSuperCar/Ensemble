using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Controls;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class DocFileView : UserControl, IViewFor<DocFileViewModel>
{
	private const double ScrollBarGap = 4d;

	// LiveMarkdown's classes on the Border of each heading, around its level (e.g., Heading2Block)
	private const string HeadingClassPrefix = "Heading";
	private const string HeadingClassSuffix = "Block";

	private DocFileViewModel? _viewModel;

	public DocFileView()
	{
		InitializeComponent();

		ScrollBarInset.ApplyGap(ContentScroll, Renderer, ScrollBarGap);
		HorizontalWheel.Apply(TabScroll);
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);

		_viewModel?.ScrollRequested -= OnScrollRequested;
		_viewModel = DataContext as DocFileViewModel;
		_viewModel?.ScrollRequested += OnScrollRequested;
	}

	private static bool IsHeadingClass(string name) =>
		name.StartsWith(HeadingClassPrefix, StringComparison.Ordinal) &&
		name.EndsWith(HeadingClassSuffix, StringComparison.Ordinal);

	// After the layout a newly loaded document needs, as its headings aren't placed yet
	private void OnScrollRequested(int? headingIndex) =>
		Dispatcher.UIThread.Post(() => ScrollTo(headingIndex), DispatcherPriority.Background);

	private void ScrollTo(int? headingIndex)
	{
		var heading = headingIndex is { } index
			? Renderer.GetVisualDescendants()
				.OfType<Border>()
				.Where(static border => border.Classes.Any(IsHeadingClass))
				.ElementAtOrDefault(index)
			: null;

		ContentScroll.Offset = new Vector(0, heading?.TranslatePoint(default, Renderer)?.Y ?? 0);
	}
}
