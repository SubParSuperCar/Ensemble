using System;
using Avalonia.Skia;
using Godot;
using SkiaSharp;
using static Estragonia.VkInterop;

// ReSharper disable MemberCanBePrivate.Global

namespace Estragonia;

/// <summary>Encapsulates a Skia surface along with the Godot texture it comes from (Vulkan backend).</summary>
internal sealed class GodotSkiaSurface(
	SKSurface skSurface,
	Texture2Drd gdTexture,
	VkImage vkImage,
	VkImageLayout lastLayout,
	RenderingDevice renderingDevice,
	double renderScaling,
	VkBarrierHelper barrierHelper
) : IGodotSkiaSurface
{
	private ulong? _clearFrame;

	public VkImage VkImage { get; } = vkImage;

	public VkImageLayout LastLayout { get; set; } = lastLayout;

	public VkBarrierHelper BarrierHelper { get; } = barrierHelper;
	public SKSurface SkSurface { get; } = skSurface;

	public Texture2Drd GdTexture { get; } = gdTexture;

	public RenderingDevice RenderingDevice { get; } = renderingDevice;

	public double RenderScaling { get; set; } = renderScaling;

	public ulong DrawCount { get; set; }

	public bool IsDisposed { get; private set; }

	public bool IsReplaced { get; set; }

	public bool IsReady => Engine.GetProcessFrames() != _clearFrame;

	SKSurface ISkiaSurface.Surface => SkSurface;

	bool ISkiaSurface.CanBlit => false;

	void ISkiaSurface.Blit(SKCanvas canvas) => throw new NotSupportedException();

	public void ClearForDisplay()
	{
		RenderingDevice.TextureClear(GdTexture.TextureRdRid, new Color(0u), 0, 1, 0, 1);
		_clearFrame = Engine.GetProcessFrames();

		// Where Godot leaves it once it has drawn it
		LastLayout = VkImageLayout.SHADER_READ_ONLY_OPTIMAL;
	}

	public void Dispose()
	{
		if (IsDisposed)
			return;

		IsDisposed = true;
		SkSurface.Dispose();
		RenderingDevice.FreeRid(GdTexture.TextureRdRid);
		GdTexture.Dispose();
	}

	public void TransitionLayoutTo(VkImageLayout newLayout)
	{
		if (LastLayout == newLayout)
			return;

		var sourceAccessMask = LastLayout switch
		{
			VkImageLayout.COLOR_ATTACHMENT_OPTIMAL => VkAccessFlags.COLOR_ATTACHMENT_WRITE_BIT,
			VkImageLayout.SHADER_READ_ONLY_OPTIMAL => VkAccessFlags.SHADER_READ_BIT,
			_ => VkAccessFlags.MEMORY_READ_BIT | VkAccessFlags.MEMORY_WRITE_BIT
		};

		var destinationAccessMask = newLayout switch
		{
			VkImageLayout.COLOR_ATTACHMENT_OPTIMAL => VkAccessFlags.COLOR_ATTACHMENT_WRITE_BIT,
			VkImageLayout.SHADER_READ_ONLY_OPTIMAL => VkAccessFlags.SHADER_READ_BIT,
			_ => VkAccessFlags.MEMORY_READ_BIT | VkAccessFlags.MEMORY_WRITE_BIT
		};

		BarrierHelper.TransitionImageLayout(VkImage, LastLayout, sourceAccessMask, newLayout, destinationAccessMask);
		LastLayout = newLayout;
	}
}
