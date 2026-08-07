using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Tool;

// TODO
[GlobalClass]
public partial class ToolManager : Node
{
	public PlaceTool Place => field ??= CreateTool<PlaceTool>();
	public DeleteTool Delete => field ??= CreateTool<DeleteTool>();

	private T CreateTool<T>() where T : ToolBase, new()
	{
		var tool = new T();
		AddChild(tool);
		return tool;
	}
}
