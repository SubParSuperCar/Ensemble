using Godot;

namespace Root.Tool;

public partial class DeleteTool : ToolBase
{
	protected override StringName ToggleAction => "tool_delete_toggle";

	protected override void OnEnable() { }
	protected override void OnDisable() { }
}
