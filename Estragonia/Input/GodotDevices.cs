using System.Collections.Concurrent;
using Avalonia.Input;
using GdInputEvent = Godot.InputEvent;

namespace Estragonia.Input;

/// <summary>Contains the various Avalonia devices created from Godot.</summary>
public static class GodotDevices
{
	/// <summary>The device identifier used by emulated devices.</summary>
	public const int EmulatedDeviceId = -1;

	private static readonly ConcurrentDictionary<int, IMouseDevice> MouseById = new();
	private static readonly ConcurrentDictionary<int, IJoypadDevice> JoypadById = new();

	/// <summary>Gets the keyboard device.</summary>
	/// <remarks>
	///     At this time, we don't support multiple keyboard devices since Avalonia needs
	///     a single one for focus management.
	/// </remarks>
	public static IKeyboardDevice Keyboard { get; } = new KeyboardDevice();

	/// <summary>Gets a mouse device for a given Godot device identifier.</summary>
	/// <param name="deviceId">The device identifier.</param>
	/// <returns>A mouse device.</returns>
	/// <remarks>
	///     The system mouse is the primary pointer: device 0 before Godot 4.7, <see cref="GdInputEvent.DeviceIdMouse" />
	///     since. Controls that only follow the primary pointer (e.g., LiveMarkdown's links) ignore any other.
	/// </remarks>
	public static IMouseDevice GetMouse(int deviceId) =>
		MouseById.GetOrAdd(
			deviceId,
			static id => new MouseDevice(new Pointer(id, PointerType.Mouse, id is 0 or (int)GdInputEvent.DeviceIdMouse))
		);

	/// <summary>Gets a joypad device for a given Godot device identifier.</summary>
	/// <param name="deviceId">The device identifier.</param>
	/// <returns>A joypad device.</returns>
	public static IJoypadDevice GetJoypad(int deviceId) =>
		JoypadById.GetOrAdd(deviceId, static id => new JoypadDevice(id));
}
