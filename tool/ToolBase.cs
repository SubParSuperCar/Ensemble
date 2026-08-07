using Godot;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable VirtualMemberNeverOverridden.Global

namespace Root.Tool;

public abstract partial class ToolBase : Node
{
	public bool IsEnabled { get; private set; }

	// ReSharper disable once EventNeverSubscribedTo.Global
	public event Action<bool>? IsEnabledChanged;

	public void Enable()
	{
		if (IsEnabled)
			return;

		IsEnabled = true;
		IsEnabledChanged?.Invoke(IsEnabled);

		OnEnable();
	}

	public void Disable()
	{
		if (!IsEnabled)
			return;

		IsEnabled = false;
		IsEnabledChanged?.Invoke(IsEnabled);

		OnDisable();
	}

	protected virtual void OnEnable() { }
	protected virtual void OnDisable() { }
}
