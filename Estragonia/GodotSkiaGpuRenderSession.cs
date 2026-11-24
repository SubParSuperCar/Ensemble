using System;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace Estragonia;

/// <summary>A render session that uses an underlying Skia surface.</summary>
internal sealed class GodotSkiaGpuRenderSession
	: ISkiaGpuRenderSessionWithPartialPresentation, IPartialPresentationFeature
{
	private readonly SurfaceContents _contents;
	private readonly ISurfaceSynchronizer _synchronizer;

	private bool _isSucceeded;

	public GodotSkiaGpuRenderSession(
		IGodotSkiaSurface surface,
		GRContext grContext,
		ISurfaceSynchronizer synchronizer,
		SurfaceContents contents
	)
	{
		Surface = surface;
		GrContext = grContext;
		_synchronizer = synchronizer;
		_contents = contents;

		// Takes the texture over from Godot, keeping what it holds (a layout transition on Vulkan)
		_synchronizer.PrepareForRendering(Surface);
	}

	public IGodotSkiaSurface Surface { get; }

	IPartialPresentationBuffer IPartialPresentationFeature.Buffer => _contents;

	// Godot shows the whole texture each frame, so there's nothing to present
	void IPartialPresentationFeature.SetDirtyRects(
		ReadOnlySpan<PixelRect> frameDirtyRects,
		ReadOnlySpan<PixelRect> bufferDirtyRects
	)
	{
	}

	void IPartialPresentationFeature.Succeeded() => _isSucceeded = true;

	public GRContext GrContext { get; }

	SKSurface ISkiaGpuRenderSession.SkSurface => Surface.SkSurface;

	double ISkiaGpuRenderSession.ScaleFactor => Surface.RenderScaling;

	GRSurfaceOrigin ISkiaGpuRenderSession.SurfaceOrigin => GRSurfaceOrigin.TopLeft;

	IPartialPresentationFeature ISkiaGpuRenderSessionWithPartialPresentation.PartialPresentation => this;

	// Finalizes rendering, which handles the flush and the layout transitions
	public void Dispose()
	{
		if (!_isSucceeded)
			_contents.Invalidate();

		_synchronizer.FinishRendering(Surface);
	}
}
