using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Skia;
using Godot;
using SkiaSharp;
using Environment = System.Environment;

namespace Estragonia;

/// <summary>Bridges the Godot Vulkan renderer with a Skia context used by Avalonia.</summary>
#pragma warning disable CA1001
internal sealed class GodotVkSkiaGpu : ISkiaGpu
#pragma warning restore CA1001
{
	private readonly VkBarrierHelper _barrierHelper;
	private readonly GRContext _grContext;
	private readonly uint _queueFamilyIndex;

	private readonly RenderingDevice _renderingDevice;

	public unsafe GodotVkSkiaGpu()
	{
		_renderingDevice = RenderingServer.GetRenderingDevice();

		if (_renderingDevice is null)
			throw new NotSupportedException("Estragonia is only supported on Vulkan renderers (Forward+ or Mobile)");

		var vkInstance =
			new VkInterop.VkInstance(GetIntPtrDriverResource(RenderingDevice.DriverResource.TopmostObject));
		var vkPhysicalDevice =
			new VkInterop.VkPhysicalDevice(GetIntPtrDriverResource(RenderingDevice.DriverResource.PhysicalDevice));
		var vkDevice = new VkInterop.VkDevice(GetIntPtrDriverResource(RenderingDevice.DriverResource.LogicalDevice));
		var vkQueue = new VkInterop.VkQueue(GetIntPtrDriverResource(RenderingDevice.DriverResource.CommandQueue));
		var vkQueueFamilyIndex =
			(uint)_renderingDevice.GetDriverResource(RenderingDevice.DriverResource.QueueFamily, default, 0UL);

		if (!TryLoadVulkanLibrary(out var vkLibrary))
			throw new DllNotFoundException("Couldn't find Vulkan loader library");

		var vkGetInstanceProcAddr =
			(delegate* unmanaged[Stdcall]<VkInterop.VkInstance, byte*, IntPtr>)NativeLibrary.GetExport(vkLibrary,
				"vkGetInstanceProcAddr");
		var vkGetDeviceProcAddr =
			(delegate* unmanaged[Stdcall]<VkInterop.VkDevice, byte*, IntPtr>)NativeLibrary.GetExport(vkLibrary,
				"vkGetDeviceProcAddr");

		var deviceApi = new VkDeviceApi(vkDevice, vkGetDeviceProcAddr);

		var vkContext = new GRVkBackendContext
		{
			VkInstance = vkInstance.Handle,
			VkPhysicalDevice = vkPhysicalDevice.Handle,
			VkDevice = vkDevice.Handle,
			VkQueue = vkQueue.Handle,
			GraphicsQueueIndex = vkQueueFamilyIndex,
			GetProcedureAddress = GetVkProcAddress
		};

		if (GRContext.CreateVulkan(vkContext) is not { } grContext)
			throw new InvalidOperationException("Couldn't create Vulkan context");

		_grContext = grContext;
		_queueFamilyIndex = vkQueueFamilyIndex;
		_barrierHelper = new VkBarrierHelper(vkDevice, vkQueue, deviceApi, vkQueueFamilyIndex);
		return;

		IntPtr GetIntPtrDriverResource(RenderingDevice.DriverResource resource)
		{
			var result = (IntPtr)_renderingDevice.GetDriverResource(resource, default, 0UL);

			return result == IntPtr.Zero
				? throw new InvalidOperationException($"Godot returned null for driver resource {resource}")
				: result;
		}

		IntPtr GetVkProcAddress(string name, IntPtr instance, IntPtr device)
		{
			Span<byte> utf8Name = stackalloc byte[128];

			// The stackalloc buffer should always be sufficient for proc names
			if (Utf8.FromUtf16(name, utf8Name[..^1], out _, out var bytesWritten) != OperationStatus.Done)
				throw new InvalidOperationException($"Invalid proc name {name}");

			utf8Name[bytesWritten] = 0;

			fixed (byte* utf8NamePtr = utf8Name)
			{
				return device != IntPtr.Zero
					? vkGetDeviceProcAddr(new VkInterop.VkDevice(device), utf8NamePtr)
					: vkGetInstanceProcAddr(new VkInterop.VkInstance(instance), utf8NamePtr);
			}
		}
	}

	public bool IsLost => _grContext.IsAbandoned;

	object? IOptionalFeatureProvider.TryGetFeature(Type featureType) => null;

	IDisposable IPlatformGraphicsContext.EnsureCurrent() => EmptyDisposable.Instance;

	// ReSharper disable once ReturnTypeCanBeNotNullable
	public IPlatformGraphicsContext? PlatformGraphicsContext => this;

#pragma warning disable CA1822
	public bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces)
#pragma warning restore CA1822
		=>
			true;

	public ISkiaGpuRenderTarget? TryCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces) =>
		surfaces.OfType<GodotSkiaSurface>().FirstOrDefault() is { } surface
			? new GodotSkiaRenderTarget(surface, _grContext, _barrierHelper)
			: null;

	// ReSharper disable once ReturnTypeCanBeNotNullable
	public IScopedResource<GRContext>? TryGetGrContext() =>
		ScopedResource<GRContext>.Create(_grContext, static () => { });

	public ISkiaSurface? TryCreateSurface(PixelSize size, ISkiaGpuRenderSession? session) =>
		session is GodotSkiaGpuRenderSession godotSession
			? CreateSurface(size, godotSession.Surface.RenderScaling)
			: null;

	public void Dispose()
	{
		_grContext.Dispose();
		_barrierHelper.Dispose();
	}

	// Logic should match volk:
	// https://github.com/godotengine/godot/blob/e4e024ab88efe74677769395886bc1b09eccbac7/thirdparty/volk/volk.c#L71-L115
	private static bool TryLoadVulkanLibrary(out IntPtr handle)
	{
		if (OperatingSystem.IsWindows())
			return TryLoadByName("vulkan-1.dll", out handle);

		if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
		{
			return TryLoadByName("libvulkan.dylib", out handle)
				   || TryLoadByName("libvulkan.1.dylib", out handle)
				   || TryLoadByName("libMoltenVK.dylib", out handle)
				   || TryLoadByPath("vulkan.framework/vulkan", out handle)
				   || TryLoadByPath("MoltenVK.framework/MoltenVK", out handle)
				   || (Environment.GetEnvironmentVariable("DYLD_FALLBACK_LIBRARY_PATH") is null
					   && TryLoadByPath("/usr/local/lib/libvulkan.dylib", out handle)
				   );
		}

		return TryLoadByName("libvulkan.so.1", out handle)
			   || TryLoadByName("libvulkan.so", out handle);

		static bool TryLoadByName(string libraryName, out IntPtr handle)
		{
			return NativeLibrary.TryLoad(libraryName, typeof(GodotVkSkiaGpu).Assembly, null, out handle);
		}

		static bool TryLoadByPath(string libraryPath, out IntPtr handle)
		{
			return NativeLibrary.TryLoad(libraryPath, out handle);
		}
	}

	public GodotSkiaSurface CreateSurface(PixelSize size, double renderScaling)
	{
		size = new PixelSize(Math.Max(size.Width, 1), Math.Max(size.Height, 1));

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
						| RenderingDevice.TextureUsageBits.CanCopyFromBit
						| RenderingDevice.TextureUsageBits.CanCopyToBit
						| RenderingDevice.TextureUsageBits.ColorAttachmentBit
		};

		var gdRdTexture = _renderingDevice.TextureCreate(gdRdTextureFormat, new RDTextureView());

		var vkImage =
			new VkInterop.VkImage(
				_renderingDevice.GetDriverResource(RenderingDevice.DriverResource.Texture, gdRdTexture, 0UL));
		if (vkImage.Handle == 0UL)
			throw new InvalidOperationException("Couldn't get Vulkan image from Godot texture");

		var vkFormat =
			(uint)_renderingDevice.GetDriverResource(RenderingDevice.DriverResource.TextureDataFormat, gdRdTexture,
				0UL);
		if (vkFormat == 0U)
			throw new InvalidOperationException("Couldn't get Vulkan format from Godot texture");

		var grVkImageInfo = new GRVkImageInfo
		{
			CurrentQueueFamily = _queueFamilyIndex,
			Format = vkFormat,
			Image = vkImage.Handle,
			ImageLayout = (uint)VkInterop.VkImageLayout.COLOR_ATTACHMENT_OPTIMAL,
			ImageTiling = (uint)VkInterop.VkImageTiling.OPTIMAL,
			ImageUsageFlags = (uint)(
				VkInterop.VkImageUsageFlags.SAMPLED_BIT |
				VkInterop.VkImageUsageFlags.TRANSFER_SRC_BIT |
				VkInterop.VkImageUsageFlags.TRANSFER_DST_BIT |
				VkInterop.VkImageUsageFlags.COLOR_ATTACHMENT_BIT
			),
			LevelCount = 1,
			SampleCount = 1,
			Protected = false,
			SharingMode = (uint)VkInterop.VkSharingMode.EXCLUSIVE
		};

		var skSurface = SKSurface.Create(
			_grContext,
			new GRBackendRenderTarget(size.Width, size.Height, grVkImageInfo),
			GRSurfaceOrigin.TopLeft,
			SKColorType.Rgba8888,
			new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal)
		);

		if (skSurface is null)
			throw new InvalidOperationException("Couldn't create Skia surface from Vulkan image");

		var gdTexture = new Texture2Drd
		{
			TextureRdRid = gdRdTexture
		};

		var surface = new GodotSkiaSurface(
			skSurface,
			gdTexture,
			vkImage,
			VkInterop.VkImageLayout.UNDEFINED,
			_renderingDevice,
			renderScaling,
			_barrierHelper
		);

		surface.TransitionLayoutTo(VkInterop.VkImageLayout.COLOR_ATTACHMENT_OPTIMAL);

		return surface;
	}
}
