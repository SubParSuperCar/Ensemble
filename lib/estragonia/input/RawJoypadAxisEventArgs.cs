using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Estragonia.Input;

/// <summary>Represents raw input event arguments related to a joypad axis.</summary>
public class RawJoypadAxisEventArgs(
	IJoypadDevice device,
	ulong timestamp,
	IInputRoot root)
	: RawInputEventArgs(device, timestamp, root);
