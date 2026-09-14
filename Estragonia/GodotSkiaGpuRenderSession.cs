using Avalonia.Skia;
using SkiaSharp;

namespace Estragonia;

/// <summary>A render session that uses an underlying Skia surface.</summary>
internal sealed class GodotSkiaGpuRenderSession : ISkiaGpuRenderSession
{
	private readonly ISurfaceSynchronizer _synchronizer;

	public GodotSkiaGpuRenderSession(IGodotSkiaSurface surface, GRContext grContext, ISurfaceSynchronizer synchronizer)
	{
		Surface = surface;
		GrContext = grContext;
		_synchronizer = synchronizer;

		// Prepare the surface for rendering (handles texture clear and layout transitions)
		_synchronizer.PrepareForRendering(Surface);
	}

	public IGodotSkiaSurface Surface { get; }

	public GRContext GrContext { get; }

	SKSurface ISkiaGpuRenderSession.SkSurface => Surface.SkSurface;

	double ISkiaGpuRenderSession.ScaleFactor => Surface.RenderScaling;

	GRSurfaceOrigin ISkiaGpuRenderSession.SurfaceOrigin => GRSurfaceOrigin.TopLeft;

	// Finalizes rendering, which handles the flush and the layout transitions
	public void Dispose() => _synchronizer.FinishRendering(Surface);
}
