using EnsembleRoot.Ui.Abstractions;
using Godot;

namespace EnsembleRoot.Ui.Services;

public sealed class DispatcherService : ISingletonObject
{
	public event Action<double>? UiProcess;
	public event Action<InputEvent>? Input;
	public event Action<int>? Notification;

	/// <summary>A mouse button went down outside the UI, e.g., on the world or a blank part of the screen.</summary>
	public event Action? PressedOutside;

	internal void OnUiProcess(double delta) => UiProcess?.Invoke(delta);
	internal void OnInput(InputEvent @event) => Input?.Invoke(@event);
	internal void OnNotification(int what) => Notification?.Invoke(what);
	internal void OnPressedOutside() => PressedOutside?.Invoke();
}
