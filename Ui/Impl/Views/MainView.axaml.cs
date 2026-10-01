using Avalonia.Controls;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;
using Godot;
#if ENSEMBLE_DEBUG
using Avalonia;
using Avalonia.Rendering;
#endif

namespace EnsembleRoot.Ui.Impl.Views;

public partial class MainView : UserControl, IViewFor<MainViewModel>
{
	public MainView()
	{
		InitializeComponent();

		var version = ProjectSettings.GetSetting("application/config/version").AsString();
		Watermark.Text =
			$"\"Ensemble\" (v{version}) (" +
#if EXPORT && !ENSEMBLE_JIT
			"AOT" +
#else
			"JIT" +
#endif
			$")\nBuilt: {BuildInfo.BuildTime}\n" +
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
