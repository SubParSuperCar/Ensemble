using Godot;

namespace Root.Ui.Impl;

public static class Dispatcher
{
	// ReSharper disable once EventNeverSubscribedTo.Global
	public static event Action<double>? Process;
	public static event Action<InputEvent>? Input;

	public static void RaiseProcess(double delta) => Process?.Invoke(delta);
	public static void RaiseInput(InputEvent input) => Input?.Invoke(input);
}
