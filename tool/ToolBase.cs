using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Tool;

public abstract partial class ToolBase : Node
{
	private ToolControl _control = null!;

	public bool IsEnabled { get; private set; }

	protected virtual StringName? ToggleAction => null;

	// ReSharper disable once EventNeverSubscribedTo.Global
	public event Action<bool>? IsEnabledChanged;

	internal void Initialize(ToolControl control)
	{
		if (_control is not null)
			throw new InvalidOperationException("Tool has already been initialized.");

		_control = control;
	}

	public void Enable() => _control.RequestEnable();
	public void Disable() => _control.RequestDisable();

	public void Toggle()
	{
		if (IsEnabled)
			Disable();
		else
			Enable();
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (ToggleAction is not null && @event.IsActionPressed(ToggleAction))
			Toggle();
	}

	internal void EnableInternal()
	{
		if (IsEnabled)
			return;

		IsEnabled = true;
		IsEnabledChanged?.Invoke(true);

		OnEnable();
	}

	internal void DisableInternal()
	{
		if (!IsEnabled)
			return;

		IsEnabled = false;
		IsEnabledChanged?.Invoke(false);

		OnDisable();
	}

	protected virtual void OnEnable() { }
	protected virtual void OnDisable() { }
}
