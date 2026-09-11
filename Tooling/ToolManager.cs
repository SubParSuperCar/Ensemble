using Godot;
using Root.Autoloading;
using Root.Tooling.Tools;
using Serilog;

namespace Root.Tooling;

[GlobalClass]
[Autoload(
	Scope = AutoloadScope.RegularClient,
	Order = sbyte.MinValue + 4,
	FailurePolicy = AutoloadFailurePolicy.AskUser)]
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

	internal static void RequestDisable(ToolBase tool) => tool.DisableInternal();

	internal void RequestEnable(ToolBase tool)
	{
		if (tool.IsEnabled || !CanEnableTools)
			return;

		if (UseMutex)
			DisableAll(tool);

		tool.EnableInternal();
	}

	private TTool CreateTool<TTool>() where TTool : ToolBase, new()
	{
		var name = typeof(TTool).Name;
		var tool = new TTool { Name = name };

		tool.Initialize(new ToolControl(this, tool));

		_tools.Add(tool);
		AddChild(tool);

		Log.Debug("Created tool: {Tool}", name);

		return tool;
	}

	private void OnIsLocalPlotSpawnedChanged(bool? _)
	{
		if (!CanEnableTools)
			DisableAll();
	}

	private void DisableAll(ToolBase? exception = null)
	{
		foreach (var tool in _tools.Where(tool => !ReferenceEquals(tool, exception)))
			tool.DisableInternal();
	}
}
