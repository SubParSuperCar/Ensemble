using Avalonia.Interactivity;
using Godot;

// ReSharper disable UnusedMember.Global

namespace Estragonia.Input;

/// <summary>Provides information about a joypad axis event.</summary>
public class JoypadAxisEventArgs(
	RoutedEvent? routedEvent,
	object? source,
	IJoypadDevice device,
	JoyAxis axis,
	float axisValue)
	: RoutedEventArgs(routedEvent, source)
{
	/// <summary>Gets the device where the event comes from.</summary>
	public IJoypadDevice Device { get; } = device;

	/// <summary>Gets the axis.</summary>
	public JoyAxis Axis { get; } = axis;

	/// <summary>
	///     Gets the current position of the joystick on the given axis.
	///     The value ranges from -1.0 to 1.0.
	///     A value of 0 means the axis is in its resting position.
	/// </summary>
	public float AxisValue { get; } = axisValue;
}
