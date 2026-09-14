using System;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace Estragonia;

/// <summary>A render target that uses an underlying Skia surface.</summary>
internal sealed class GodotSkiaRenderTarget(
	IGodotSkiaSurface surface,
	GRContext grContext,
	ISurfaceSynchronizer synchronizer
) : ISkiaGpuRenderTarget
{
	private readonly double _renderScaling = surface.RenderScaling;

	private bool IsCorrupted =>
		surface.IsDisposed || grContext.IsAbandoned || !_renderScaling.Equals(surface.RenderScaling);

	public PlatformRenderTargetState State =>
		IsCorrupted ? PlatformRenderTargetState.Corrupted : PlatformRenderTargetState.Ready;

	public ISkiaGpuRenderSession BeginRenderingSession(IRenderTarget.RenderTargetSceneInfo sceneInfo) =>
		new GodotSkiaGpuRenderSession(surface, grContext, synchronizer);

	void IDisposable.Dispose()
	{
	}
}
