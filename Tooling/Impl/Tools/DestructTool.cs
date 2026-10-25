using EnsembleRoot.Common.Input;
using EnsembleRoot.GdCore.Assets;
using Godot;
using Serilog;

namespace EnsembleRoot.Tooling.Tools;

public partial class DestructTool : ToolBase
{
	public static readonly StringName ToggleAction = "tool_destruct_toggle";
	public static readonly Color Theme = Colors.Red;

	private readonly SelectionHighlights _highlights = new() { Name = "Highlights", Tint = Theme };

	public override Color ThemeColor => Theme;
	public override bool UsesSelection => true;

	public override void _Ready() => AddChild(_highlights);

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsEnabled || InputSink.IsSunk || !ToolCommon.IsTriggerPressed(@event) || LocalPlot is not { } plot)
			return;

		var targets = Selector.Trigger(_highlights.HoveredId, Input.IsActionPressed(ToolCommon.SelectionToggleAction));

		GdInstance[] instances = [.. targets.Select(plot.Instances.GetInstance).OfType<GdInstance>()];

		if (instances.Length is 0)
			return;

		UndoRedo.Remove(instances);
		ToolCommon.PlaySound(ToolCommon.AffirmSound);

		Log.Verbose("Submitted removal of {Count} instance(s)", instances.Length);
		_highlights.ClearHover();
	}

	protected override void OnEnable() => _highlights.Attach(Selector);
	protected override void OnDisable() => _highlights.Detach();
}
