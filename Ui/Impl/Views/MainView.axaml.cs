using Avalonia.Controls;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;
#if ENSEMBLE_DEBUG
using Avalonia;
using Avalonia.Rendering;
#endif

namespace EnsembleRoot.Ui.Impl.Views;

public sealed partial class MainView : UserControl, IViewFor<MainViewModel>
{
	public MainView()
	{
		InitializeComponent();

		Watermark.Text =
			$"\"Ensemble\" (v{GameVersion}) (" +
#if EXPORT && !ENSEMBLE_JIT
			"AOT"
#else
			"JIT"
#endif
			+ $")\nBuilt: {BuildInfo.BuildTime}\n" +
			"By: @SubParSuperCar & Contributors\n" +
			$"At: {GitHubRepoUrl[HttpsScheme.Length..]}";
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
