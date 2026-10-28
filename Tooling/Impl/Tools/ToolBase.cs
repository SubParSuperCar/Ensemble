using Godot;

namespace EnsembleRoot.Tooling.Tools;

public abstract partial class ToolBase : Node
{
	private ToolManager _manager = null!;

	public bool IsEnabled { get; private set; }

	public abstract Color ThemeColor { get; }

	public virtual bool UsesSelection => false;

	protected MultiSelector Selector => _manager.Selector;
	protected UndoRedo UndoRedo => _manager.UndoRedo;

	public void Enable() => _manager.RequestEnable(this);
	public void Disable() => SetEnabled(false);

	internal void Initialize(ToolManager manager) => _manager = manager;

	internal void SetEnabled(bool isEnabled)
	{
		if (IsEnabled == isEnabled)
			return;

		IsEnabled = isEnabled;
		_manager.OnToolIsEnabledChanged(this, isEnabled);

		if (isEnabled)
			OnEnable();
		else
			OnDisable();
	}

	protected virtual void OnEnable() { }
	protected virtual void OnDisable() { }
}
