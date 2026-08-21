using Avalonia.Input;

namespace Estragonia.Input;

/// <summary>Represents a joypad (game controller) device.</summary>
public interface IJoypadDevice : IInputDevice
{
	/// <summary>Gets an identifier uniquely identifying the device (-1 if the device is emulated).</summary>
	// ReSharper disable once UnusedMemberInSuper.Global
	// ReSharper disable once UnusedMember.Global
	int Id { get; }
}
