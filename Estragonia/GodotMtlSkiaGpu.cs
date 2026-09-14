using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Skia;
using Godot;
using SkiaSharp;

namespace Estragonia;

/// <summary>Bridges the Godot Metal renderer with a Skia context used by Avalonia.</summary>
internal sealed class GodotMtlSkiaGpu : IGodotSkiaGpu
{
	private readonly GRContext _grContext;
	private readonly IntPtr _mtlQueue;
	private readonly RenderingDevice _renderingDevice;
	private readonly MtlSynchronizer _synchronizer;

	public GodotMtlSkiaGpu()
	{
		_renderingDevice = RenderingServer.GetRenderingDevice();

		if (_renderingDevice is null)
			throw new NotSupportedException("Estragonia is only supported on Forward+ or Mobile renderers");

		// Get the Metal device and command queue from Godot
		var mtlDevice = (IntPtr)_renderingDevice.GetDriverResource(
			RenderingDevice.DriverResource.LogicalDevice,
			default,
			0UL
		);
		if (mtlDevice == IntPtr.Zero)
			throw new InvalidOperationException("Godot returned null for the Metal device");

		_mtlQueue = (IntPtr)_renderingDevice.GetDriverResource(
			RenderingDevice.DriverResource.CommandQueue,
			default,
			0UL
		);
		if (_mtlQueue == IntPtr.Zero)
			throw new InvalidOperationException("Godot returned null for the Metal command queue");

		// Create the Metal GRContext through native interop
		_grContext = MtlInterop.CreateMetalContext(mtlDevice, _mtlQueue)
			?? throw new InvalidOperationException("Couldn't create Metal context");

		_synchronizer = new MtlSynchronizer();
	}

	public bool IsLost => _grContext.IsAbandoned;

	public IPlatformGraphicsContext PlatformGraphicsContext => this;

	object? IOptionalFeatureProvider.TryGetFeature(Type featureType) => null;

	IDisposable IPlatformGraphicsContext.EnsureCurrent() => EmptyDisposable.Instance;

	public bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces) => true;

	public ISkiaGpuRenderTarget? TryCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces) =>
		surfaces.OfType<GodotSkiaSurfaceMetal>().FirstOrDefault() is { } surface
			? new GodotSkiaRenderTarget(surface, _grContext, _synchronizer)
			: null;

	public IScopedResource<GRContext> TryGetGrContext() =>
		ScopedResource<GRContext>.Create(_grContext, static () => { });

	public ISkiaSurface? TryCreateSurface(PixelSize size, ISkiaGpuRenderSession? session) =>
		session is GodotSkiaGpuRenderSession godotSession
			? CreateSurface(size, godotSession.Surface.RenderScaling)
			: null;

	public IGodotSkiaSurface CreateSurface(PixelSize size, double renderScaling)
	{
		size = new PixelSize(Math.Max(size.Width, 1), Math.Max(size.Height, 1));

		// The Godot texture used for display needs ColorAttachment to be rendered into
		var gdRdTextureFormat = new RDTextureFormat
		{
			Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
			TextureType = RenderingDevice.TextureType.Type2D,
			Width = (uint)size.Width,
			Height = (uint)size.Height,
			Depth = 1,
			ArrayLayers = 1,
			Mipmaps = 1,
			Samples = RenderingDevice.TextureSamples.Samples1,
			UsageBits = RenderingDevice.TextureUsageBits.SamplingBit
				| RenderingDevice.TextureUsageBits.ColorAttachmentBit
				| RenderingDevice.TextureUsageBits.CanCopyFromBit
				| RenderingDevice.TextureUsageBits.CanCopyToBit
				| RenderingDevice.TextureUsageBits.CanUpdateBit
		};

		var gdRdTexture = _renderingDevice.TextureCreate(gdRdTextureFormat, new RDTextureView());

		// Get the native Metal texture handle from Godot
		var gdMetalTexture = (IntPtr)_renderingDevice.GetDriverResource(
			RenderingDevice.DriverResource.Texture,
			gdRdTexture,
			0UL
		);

		var gdTexture = new Texture2Drd
		{
			TextureRdRid = gdRdTexture
		};

		// Try zero-copy first: a Skia surface that wraps Godot's Metal texture directly
		if (gdMetalTexture != IntPtr.Zero)
		{
			var surface = TryCreateZeroCopySurface(gdMetalTexture, size, gdTexture, renderScaling);

			if (surface is not null)
				return surface;
		}

		// Fall back to a Skia-owned GPU surface, which requires a copy to the Godot texture
		var imageInfo = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
		var skSurface = SKSurface.Create(
			_grContext,
			true,
			imageInfo,
			1,
			GRSurfaceOrigin.TopLeft,
			new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal),
			false
		);

		if (skSurface is null)
		{
			GD.PrintErr("[Estragonia Metal] Couldn't create a Skia GPU surface, falling back to raster");
			skSurface = SKSurface.Create(imageInfo);
		}

		if (skSurface is null)
			throw new InvalidOperationException("Couldn't create Skia surface");

		return new GodotSkiaSurfaceMetal(
			skSurface,
			gdTexture,
			_renderingDevice,
			renderScaling,
			_mtlQueue,
			gdMetalTexture,
			size.Width,
			size.Height
		);
	}

	public void Dispose()
	{
		_grContext.Dispose();
		_synchronizer.Dispose();
	}

	private GodotSkiaSurfaceMetal? TryCreateZeroCopySurface(
		IntPtr gdMetalTexture,
		PixelSize size,
		Texture2Drd gdTexture,
		double renderScaling
	)
	{
		try
		{
			// Wrap Godot's Metal texture in a backend texture
			var backendTexture = MtlInterop.CreateMetalBackendTexture(size.Width, size.Height, false, gdMetalTexture);

			if (backendTexture is null)
				return null;

			// Create a Skia surface that renders directly to Godot's texture
			var skSurface = SKSurface.Create(_grContext, backendTexture, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888);

			// ReSharper disable once InvertIf -- the guard clause keeps the disposal next to its failure case
			if (skSurface is null)
			{
				backendTexture.Dispose();
				return null;
			}

			return new GodotSkiaSurfaceMetal(
				skSurface,
				gdTexture,
				_renderingDevice,
				renderScaling,
				_mtlQueue,
				gdMetalTexture,
				size.Width,
				size.Height,
				true,
				backendTexture
			);
		}
		catch
		{
			return null;
		}
	}
}
