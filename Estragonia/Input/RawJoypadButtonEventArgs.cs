using Avalonia.Input;
using Avalonia.Input.Raw;
using Godot;

namespace Estragonia.Input;

/// <summary>Represents raw input event arguments related to a joypad button.</summary>
public class RawJoypadButtonEventArgs(
	IJoypadDevice device,
	ulong timestamp,
	IInputRoot root,
	RawJoypadButtonEventType type,
	JoyButton button)
	: RawInputEventArgs(device, timestamp, root)
{
	/// <summary>Gets the associated device.</summary>
	// ReSharper disable once UnusedMember.Global
	public new IJoypadDevice Device => (IJoypadDevice)base.Device;

	/// <summary>Gets whether the button is pressed or released.</summary>
	public RawJoypadButtonEventType Type { get; } = type;

	/// <summary>Gets the button that was pressed or released.</summary>
	public JoyButton Button { get; } = button;
}
