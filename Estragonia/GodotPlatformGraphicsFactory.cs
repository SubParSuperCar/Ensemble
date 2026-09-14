using System;
using Godot;

namespace Estragonia;

/// <summary>Factory for creating the appropriate platform graphics implementation.</summary>
internal static class GodotPlatformGraphicsFactory
{
	private const string MacOsDriverSetting = "rendering/rendering_device/driver.macos";

	/// <summary>Creates the appropriate platform graphics implementation based on the current renderer.</summary>
	/// <returns>A Vulkan or Metal platform graphics implementation.</returns>
	public static IGodotPlatformGraphics Create()
	{
		if (RenderingServer.GetRenderingDevice() is null)
			throw new NotSupportedException("Estragonia requires the Forward+ or Mobile renderer");

		return ShouldUseMetal()
			? new GodotMtlPlatformGraphics()
			: new GodotVkPlatformGraphics();
	}

	/// <summary>Determines whether the Metal backend should be used.</summary>
	private static bool ShouldUseMetal()
	{
		// Metal is only available on Apple platforms
		if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsIOS())
			return false;

		// Vulkan (through MoltenVK) can still be requested explicitly in the project settings
		var settings = ProjectSettings.Singleton;

		return !settings.HasSetting(MacOsDriverSetting)
			|| settings.GetSetting(MacOsDriverSetting).AsString() != "vulkan";
	}
}
