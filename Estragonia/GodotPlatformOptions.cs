using Avalonia;

namespace Estragonia;

// ReSharper disable once InvalidXmlDocComment
/// <summary>Options for the Godot platform backend, set through <see cref="AppBuilder.With{T}" />.</summary>
public sealed class GodotPlatformOptions
{
	/// <summary>
	///     Gets or sets the minimum time, in seconds, between two Avalonia process ticks. Every
	///     <see cref="AvaloniaControl" /> and window shares the same ticks, independently of Godot's frame rate.
	///     Defaults to 0, which processes Avalonia on every Godot frame.
	/// </summary>
	// ReSharper disable once PropertyCanBeMadeInitOnly.Global
	public double ProcessInterval { get; set; }
}
