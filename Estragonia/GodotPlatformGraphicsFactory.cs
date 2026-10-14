using System;
using Godot;

namespace Estragonia;

/// <summary>Factory for creating the appropriate platform graphics implementation.</summary>
internal static class GodotPlatformGraphicsFactory
{
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

	/// <summary>
	///     Determines whether the Metal backend should be used, from the driver Godot actually started, which can differ
	///     from the project setting (e.g., Godot falling back to Vulkan through MoltenVK).
	/// </summary>
	private static bool ShouldUseMetal() => RenderingServer.GetCurrentRenderingDriverName() == "metal";
}
