using System.Diagnostics.CodeAnalysis;
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
	public bool IsCorrupted
		=> surface.IsDisposed || grContext.IsAbandoned || _renderScaling != surface.RenderScaling;

	public ISkiaGpuRenderSession BeginRenderingSession() =>
		new GodotSkiaGpuRenderSession(surface, grContext, synchronizer);

	public void Dispose()
	{
	}
}
