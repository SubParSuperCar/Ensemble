using Godot;
using Root.Autoloading;
using Serilog;

namespace Root.Tooling;

[GlobalClass]
[Autoload(Scope = AutoloadScope.Client, Order = sbyte.MinValue + 4, FailurePolicy = AutoloadFailurePolicy.AskUser)]
public partial class ToolManager : Node, IAutoload
{
	private readonly List<ToolBase> _tools = [];

	public static ToolManager? Instance
	{
		get;
		private set
		{
			field = value;

			Log.Debug("{Class}.{Member} set. (Hash={Hash})",
				nameof(ToolManager),
				nameof(Instance),
				value?.GetHashCode());
		}
	}

	public bool UseMutex { get; set; } = true;

	public ConstructTool Construct => field ??= CreateTool<ConstructTool>();
	public DestructTool Destruct => field ??= CreateTool<DestructTool>();

	private static bool CanEnableTools => IsLocalPlotSpawned is false;

	public void Initialize() => IsLocalPlotSpawnedChanged += OnIsLocalPlotSpawnedChanged;

	public override void _EnterTree() => Instance = this;

	public override void _ExitTree()
	{
		IsLocalPlotSpawnedChanged -= OnIsLocalPlotSpawnedChanged;

		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

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

	private void OnIsLocalPlotSpawnedChanged(bool? _)
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
