using Godot;

namespace EnsembleRoot.Common.Input;

/// <summary>Captures the mouse while keeping its last visible position as the pointer position.</summary>
#pragma warning disable CA1720
public static class Pointer
#pragma warning restore CA1720
{
	private static Vector2? _capturedPosition;

	public static bool IsCaptured => _capturedPosition is not null;

	public static Vector2 GetPosition(Viewport viewport) => _capturedPosition ?? viewport.GetMousePosition();

	public static void Capture(Viewport viewport)
	{
		if (IsCaptured)
			return;

		_capturedPosition = viewport.GetMousePosition();
		Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Captured;
	}

	public static void Release()
	{
		if (_capturedPosition is not { } position)
			return;

		_capturedPosition = null;
		Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Visible;
		Godot.Input.WarpMouse(position);
	}
}
