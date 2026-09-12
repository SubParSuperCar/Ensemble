using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace Estragonia;

/// <summary>A render target that uses an underlying Skia surface.</summary>
internal sealed class GodotSkiaRenderTarget(
	IGodotSkiaSurface surface,
	GRContext grContext,
	ISurfaceSynchronizer synchronizer)
	: ISkiaGpuRenderTarget
{
	private readonly double _renderScaling = surface.RenderScaling;

	[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Doesn't affect correctness")]
	private bool IsCorrupted => surface.IsDisposed || grContext.IsAbandoned || _renderScaling != surface.RenderScaling;

	public PlatformRenderTargetState State =>
		IsCorrupted ? PlatformRenderTargetState.Corrupted : PlatformRenderTargetState.Ready;

	public ISkiaGpuRenderSession BeginRenderingSession(IRenderTarget.RenderTargetSceneInfo sceneInfo)
	{
		return new GodotSkiaGpuRenderSession(surface, grContext, synchronizer);
	}

	void IDisposable.Dispose()
	{
	}
}