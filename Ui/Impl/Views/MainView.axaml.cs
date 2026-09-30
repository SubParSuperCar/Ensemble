using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;

namespace EnsembleRoot.Ui.Impl.Views;

public partial class MainView : UserControl, IViewFor<MainViewModel>
{
	public MainView()
	{
		InitializeComponent();
	}

#if ENSEMBLE_DEBUG
	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);

		if (TopLevel.GetTopLevel(this) is { } topLevel)
			topLevel.RendererDiagnostics.DebugOverlays =
				RendererDebugOverlays.Fps |
				RendererDebugOverlays.RenderTimeGraph |
				RendererDebugOverlays.LayoutTimeGraph;
	}
#endif
}
