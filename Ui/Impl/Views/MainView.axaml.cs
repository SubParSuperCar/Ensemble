using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.ViewModels;
using Godot;
#if DEBUG
using Avalonia.Rendering;
#endif

namespace EnsembleRoot.Ui.Views;

public sealed partial class MainView : UserControl, IViewFor<MainViewModel>
{
	private const string DimmedClass = "dimmed";

	private static readonly TimeSpan HoverPollInterval = TimeSpan.FromSeconds(1 / 8d);

	private readonly DispatcherTimer _hoverTimer = new() { Interval = HoverPollInterval };
	private TopLevel? _topLevel;

	public MainView()
	{
		InitializeComponent();

		_hoverTimer.Tick += OnHoverTimerTick;

		Watermark.Text =
			$"\"Ensemble\" (v{GameVersion}) ({(BuildInfo.IsNativeAot ? "AOT" : "JIT")})\n" +
			$"Built: {BuildInfo.BuildTime}\n" +
			"By: @SubParSuperCar & Contributors\n" +
			$"At: {GitHubRepoUrl[HttpsScheme.Length..]}";
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);

		_topLevel = TopLevel.GetTopLevel(this);
		_hoverTimer.Start();

#if DEBUG
		_topLevel?.RendererDiagnostics.DebugOverlays =
			RendererDebugOverlays.Fps |
			RendererDebugOverlays.RenderTimeGraph |
			RendererDebugOverlays.LayoutTimeGraph;
#endif
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromVisualTree(e);

		_hoverTimer.Stop();
		_topLevel = null;
	}

	// Dims the watermark while hovered, so it's less in the way. Polls Godot's cursor, since the watermark ignores hit
	// tests, so it gets no pointer events.
	private void OnHoverTimerTick(object? sender, EventArgs e)
	{
		if (_topLevel is null || Engine.GetMainLoop() is not SceneTree tree)
			return;

		var cursor = tree.Root.GetMousePosition();
		var origin = Watermark.TranslatePoint(default, _topLevel) ?? default;
		var bounds = new Rect(origin, Watermark.Bounds.Size);

		Watermark.Classes.Set(DimmedClass, bounds.Contains(new Point(cursor.X, cursor.Y) / _topLevel.RenderScaling));
	}
}
