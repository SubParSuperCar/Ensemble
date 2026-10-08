using Godot;

namespace EnsembleRoot.Tooling.Tools;

public abstract partial class ToolBase : Node
{
	private ToolControl _control = null!;

	public bool IsEnabled { get; private set; }

	public abstract Color ThemeColor { get; }

	public void Enable() => _control.RequestEnable();
	public void Disable() => _control.RequestDisable();

	internal void Initialize(ToolControl control)
	{
		if (_control is not null)
			throw new InvalidOperationException("Tool has already been initialized.");

		_control = control;
	}

	internal void EnableInternal()
	{
		if (IsEnabled)
			return;

		IsEnabled = true;
		_control.NotifyIsEnabledChanged(true);

		OnEnable();
	}

	internal void DisableInternal()
	{
		if (!IsEnabled)
			return;

		IsEnabled = false;
		_control.NotifyIsEnabledChanged(false);

		OnDisable();
	}

	protected virtual void OnEnable() { }
	protected virtual void OnDisable() { }
}
