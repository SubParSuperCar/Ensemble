using Godot;

namespace Root.Tooling;

public partial class DestructTool : ToolBase
{
	protected override StringName ToggleAction => "tool_destruct_toggle";

	protected override void OnEnable() { }
	protected override void OnDisable() { }
}
