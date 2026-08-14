namespace Root.Tool;

internal sealed class ToolControl(ToolManager manager, ToolBase tool)
{
	public void RequestEnable() => manager.RequestEnable(tool);
	public void RequestDisable() => ToolManager.RequestDisable(tool);
}
