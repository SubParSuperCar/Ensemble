using Avalonia.Platform.Surfaces;
using Avalonia.Skia;
using Godot;
using SkiaSharp;

namespace Estragonia;

/// <summary>Interface for Godot Skia surfaces that can be used for rendering.</summary>
internal interface IGodotSkiaSurface : ISkiaSurface, IPlatformRenderSurface
{
	/// <summary>Gets the underlying Skia surface.</summary>
	SKSurface SkSurface { get; }

	/// <summary>Gets the Godot texture.</summary>
	Texture2Drd GdTexture { get; }

	/// <summary>Gets the Godot rendering device.</summary>
	RenderingDevice RenderingDevice { get; }

	/// <summary>Gets or sets the render scaling factor.</summary>
	double RenderScaling { get; set; }

	/// <summary>Gets or sets the number of times this surface has been drawn to.</summary>
	ulong DrawCount { get; set; }

	/// <summary>Gets whether this surface has been disposed.</summary>
	bool IsDisposed { get; }

	/// <summary>Gets or sets whether a newer surface has replaced this one, so Skia no longer draws to it.</summary>
	bool IsReplaced { get; set; }

	/// <summary>Clears the texture through Godot, which must draw it in the same frame.</summary>
	/// <remarks>
	///     Godot runs its commands when it submits the frame, after Skia's, so the surface isn't ready for Skia until
	///     the next frame. Without the clear, Godot sees a texture it never wrote, which some GPUs (e.g., AMD's) show
	///     corrupted.
	/// </remarks>
	void ClearForDisplay();
}
