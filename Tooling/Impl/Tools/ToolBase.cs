using Godot;
using Serilog;

namespace EnsembleRoot.Tooling.Tools;

public abstract partial class ToolBase : Node
{
	private ToolManager _manager = null!;

	public bool IsEnabled { get; private set; }

	public abstract Color ThemeColor { get; }

	protected virtual bool UsesSelection => false;

	/// <summary>Whether the marquee works while the tool is enabled; a tool that selects selects with it.</summary>
	public virtual bool UsesMarquee => UsesSelection;

	protected MultiSelector Selector => _manager.Selector;
	protected UndoRedo UndoRedo => _manager.UndoRedo;

	public void Enable() => _manager.RequestEnable(this);
	public void Disable() => SetEnabled(false);

	internal void Initialize(ToolManager manager) => _manager = manager;

	/// <summary>Whether the tool works on an asset's instances: only these can be hovered or boxed with it.</summary>
	internal virtual bool CanTarget(int assetId) => true;

	internal virtual void OnMarqueeFinished(IReadOnlyList<int> instanceIds, bool isToggling)
	{
		if (isToggling)
			Selector.Toggle(instanceIds);
		else
			Selector.Set(instanceIds);

		Log.Verbose("Marquee-selected {Count} instance(s)", instanceIds.Count);
	}

	// A marquee press released without a drag
	internal virtual void OnMarqueeClicked() { }

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
