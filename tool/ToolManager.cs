using Godot;
using Root.Common.Globals;
using Serilog;

namespace Root.Tool;

[GlobalClass]
public partial class ToolManager : Node
{
	private readonly List<ToolBase> _tools = [];

	public static ToolManager? Instance
	{
		get;
		private set
		{
			field = value;

			Log.Debug("{Class}.{Member} set (hash: {Hash})",
				nameof(ToolManager),
				nameof(Instance),
				value?.GetHashCode());
		}
	}

	public bool UseMutex { get; set; } = true;

	public PlaceTool Place => field ??= CreateTool<PlaceTool>();
	public DeleteTool Delete => field ??= CreateTool<DeleteTool>();

	private static bool CanEnableTools => GContext.IsPlotSpawned is false;

	public override void _EnterTree() => Instance = this;

	public override void _ExitTree()
	{
		GContext.IsPlotSpawnedChanged -= OnIsPlotSpawnedChanged;

		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public override void _Ready() => GContext.IsPlotSpawnedChanged += OnIsPlotSpawnedChanged;

	private T CreateTool<T>() where T : ToolBase, new()
	{
		var tool = new T();
		tool.Initialize(new ToolControl(this, tool));

		_tools.Add(tool);
		AddChild(tool);

		Log.Debug("Created tool: {Tool}", tool.GetType().Name);

		return tool;
	}

	internal void RequestEnable(ToolBase tool)
	{
		if (tool.IsEnabled || !CanEnableTools)
			return;

		if (UseMutex)
			DisableAllExcept(tool);

		tool.EnableInternal();
	}

	internal static void RequestDisable(ToolBase tool) => tool.DisableInternal();

	private void OnIsPlotSpawnedChanged(bool? _)
	{
		if (!CanEnableTools)
			DisableAll();
	}

	private void DisableAll()
	{
		foreach (var tool in _tools)
			tool.DisableInternal();
	}

	private void DisableAllExcept(ToolBase exception)
	{
		foreach (var tool in _tools.Where(tool => !ReferenceEquals(tool, exception)))
			tool.DisableInternal();
	}
}
