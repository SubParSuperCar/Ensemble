using System;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace Estragonia;

/// <summary>A render target that uses an underlying Skia surface.</summary>
/// <remarks>
///     The surface keeps what was drawn into it, so the compositor draws each frame's changes straight into it,
///     instead of into a layer of its own that it then copies whole.
/// </remarks>
internal sealed class GodotSkiaRenderTarget(
	IGodotSkiaSurface surface,
	GRContext grContext,
	ISurfaceSynchronizer synchronizer
) : ISkiaGpuRenderTarget
{
	private static readonly RenderTargetProperties DirectProperties = new()
	{
		SupportsPartialPresentation = true, IsSuitableForDirectRendering = true
	};

	private readonly double _renderScaling = surface.RenderScaling;

	private SurfaceContents _contents = new();

	private bool IsCorrupted =>
		surface.IsDisposed || surface.IsReplaced || grContext.IsAbandoned ||
		!_renderScaling.Equals(surface.RenderScaling);

	public PlatformRenderTargetState State =>
		IsCorrupted ? PlatformRenderTargetState.Corrupted : PlatformRenderTargetState.Ready;

#pragma warning disable CA1822
	RenderTargetProperties ISkiaGpuRenderTarget.Properties => DirectProperties;
#pragma warning restore CA1822

	public ISkiaGpuRenderSession BeginRenderingSession(IRenderTarget.RenderTargetSceneInfo sceneInfo)
	{
		// Invalidation is permanent, so what a failed frame left behind is new contents, which the compositor redraws
		if (!_contents.IsValid)
			_contents = new SurfaceContents();

		return new GodotSkiaGpuRenderSession(surface, grContext, synchronizer, _contents);
	}

	void IDisposable.Dispose()
	{
	}
}
