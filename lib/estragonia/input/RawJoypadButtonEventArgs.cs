using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Estragonia.Input;

/// <summary>Represents raw input event arguments related to a joypad button.</summary>
public class RawJoypadButtonEventArgs(
	IJoypadDevice device,
	ulong timestamp,
	IInputRoot root,
	RawJoypadButtonEventType type)
	: RawInputEventArgs(device, timestamp, root)
{
	/// <summary>Gets whether the button is pressed or released.</summary>
	public RawJoypadButtonEventType Type { get; } = type;
}
