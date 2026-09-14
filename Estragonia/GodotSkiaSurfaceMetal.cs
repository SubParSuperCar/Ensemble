using System;
using Avalonia.Skia;
using Godot;
using SkiaSharp;

namespace Estragonia;

/// <summary>Encapsulates a Skia surface along with the Godot texture it comes from (Metal backend).</summary>
internal sealed class GodotSkiaSurfaceMetal(
	SKSurface skSurface,
	Texture2Drd gdTexture,
	RenderingDevice renderingDevice,
	double renderScaling,
	IntPtr commandQueue,
	IntPtr gdMetalTexture,
	int width,
	int height,
	bool isZeroCopy = false,
	GRBackendTexture? backendTexture = null
) : IGodotSkiaSurface
{
	/// <summary>Gets the Metal command queue handle used for GPU blitting.</summary>
	public IntPtr CommandQueue { get; } = commandQueue;

	/// <summary>Gets the Godot texture's native Metal handle.</summary>
	public IntPtr GdMetalTexture { get; } = gdMetalTexture;

	/// <summary>Gets the width of the surface in pixels.</summary>
	public int Width { get; } = width;

	/// <summary>Gets the height of the surface in pixels.</summary>
	public int Height { get; } = height;

	/// <summary>Gets whether this surface renders directly to Godot's texture (no copy needed).</summary>
	public bool IsZeroCopy { get; } = isZeroCopy;

	/// <summary>Gets the backend texture wrapping Godot's Metal texture (only used in zero-copy mode).</summary>
	private GRBackendTexture? BackendTexture { get; } = backendTexture;

	public SKSurface SkSurface { get; } = skSurface;

	public Texture2Drd GdTexture { get; } = gdTexture;

	public RenderingDevice RenderingDevice { get; } = renderingDevice;

	public double RenderScaling { get; set; } = renderScaling;

	public ulong DrawCount { get; set; }

	public bool IsDisposed { get; private set; }

	SKSurface ISkiaSurface.Surface => SkSurface;

	bool ISkiaSurface.CanBlit => false;

	void ISkiaSurface.Blit(SKCanvas canvas) => throw new NotSupportedException();

	public void Dispose()
	{
		if (IsDisposed)
			return;

		IsDisposed = true;
		SkSurface.Dispose();
		BackendTexture?.Dispose();
		RenderingDevice.FreeRid(GdTexture.TextureRdRid);
		GdTexture.Dispose();
	}
}
