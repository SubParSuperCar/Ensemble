using Avalonia.Interactivity;
using Godot;

namespace Estragonia.Input;

/// <summary>Provides information about a joypad button event.</summary>
public class JoypadButtonEventArgs(RoutedEvent? routedEvent, object? source, IJoypadDevice device, JoyButton button)
	: RoutedEventArgs(routedEvent, source)
{
	/// <summary>Gets the device where the event comes from.</summary>
	public IJoypadDevice Device { get; } = device;

	/// <summary>Gets the button that was pressed or released.</summary>
	public JoyButton Button { get; } = button;
}
