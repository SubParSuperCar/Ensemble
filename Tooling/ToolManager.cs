using EnsembleRoot.Autoloading;
using EnsembleRoot.Common.Input;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Scripts.Plots.Outlines;
using EnsembleRoot.Tooling.Tools;
using Godot;
using Serilog;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Tooling;

[GlobalClass]
[Autoload(
	Scope = AutoloadScope.RegularClient,
	Order = AutoloadOrder.Early + 3,
	FailurePolicy = AutoloadFailurePolicy.AskUser)]
public partial class ToolManager : Node, IAutoload
{
	private static readonly StringName UndoAction = "tool_history_undo";
	private static readonly StringName RedoAction = "tool_history_redo";

	private readonly List<ToolBase> _enabledTools = [];
	private readonly List<ToolBase> _spawnDisabledTools = [];
	private readonly Dictionary<Type, ToolBase> _tools = [];

	private bool _isEnteringSpawnedPlot;

	public static ToolManager? Instance
	{
		get;
		private set
		{
			field = value;
			Log.Debug(
				"{Class}.{Member} set (Hash={Hash})", nameof(ToolManager), nameof(Instance), value?.GetHashCode());
		}
	}

	public bool UseMutex { get; set; } = true;

	/// <summary>Whether the tools disabled by spawning the local plot are enabled again once it despawns.</summary>
	public bool RestoreOnDespawn { get; set; }

	public ConstructTool Construct => Get<ConstructTool>();
	public DestructTool Destruct => Get<DestructTool>();
	public PropertyTool Property => Get<PropertyTool>();

	public MultiSelector Selector { get; } = new();
	public UndoRedo UndoRedo { get; } = new();
	public MarqueeSelector Marquee { get; } = new() { Name = nameof(MarqueeSelector) };

	internal IReadOnlyList<ToolBase> EnabledTools => _enabledTools;

	private static bool CanEnableTools => IsLocalPlotSpawned is false;

	public void Initialize()
	{
		Selector.Initialize();
		UndoRedo.Initialize();

		Marquee.Initialize(this);
		AddChild(Marquee);

		LocalPlotChanged += OnLocalPlotChanged;
		IsLocalPlotSpawnedChanged += OnIsLocalPlotSpawnedChanged;
		GSessionManager.ActionRejected += OnActionRejected;
	}

	/// <summary>Raised for every tool, so observers need not create tools just to watch them.</summary>
	public event Action<ToolBase, bool>? ToolIsEnabledChanged;

	public override void _EnterTree() => Instance = this;

	public override void _ExitTree()
	{
		Selector.Dispose();
		UndoRedo.Dispose();

		LocalPlotChanged -= OnLocalPlotChanged;
		IsLocalPlotSpawnedChanged -= OnIsLocalPlotSpawnedChanged;
		GSessionManager.ActionRejected -= OnActionRejected;

		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (InputSink.IsSunk)
			return;

		if (@event.IsActionPressed(ConstructTool.ToggleAction))
			Toggle<ConstructTool>();
		else if (@event.IsActionPressed(DestructTool.ToggleAction))
			Toggle<DestructTool>();
		else if (@event.IsActionPressed(PropertyTool.ToggleAction))
			Toggle<PropertyTool>();
		else if (@event.IsActionPressed(UndoAction))
			UndoRedo.Undo();
		else if (@event.IsActionPressed(RedoAction))
			UndoRedo.Redo();
	}

	public TTool Get<TTool>() where TTool : ToolBase, new() =>
		_tools.TryGetValue(typeof(TTool), out var tool) ? (TTool)tool : CreateTool<TTool>();

	public bool IsEnabled<TTool>() where TTool : ToolBase =>
		_tools.TryGetValue(typeof(TTool), out var tool) && tool.IsEnabled;

	/// <summary>Toggles a tool, creating it only if it can actually be enabled.</summary>
	public void Toggle<TTool>() where TTool : ToolBase, new()
	{
		if (IsEnabled<TTool>())
			Get<TTool>().Disable();
		else if (CanEnableTools)
			Get<TTool>().Enable();
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

	internal void OnToolIsEnabledChanged(ToolBase tool, bool isEnabled)
	{
		_enabledTools.Remove(tool);

		if (isEnabled)
			_enabledTools.Add(tool);

		PlotOutlines.ToolColor = _enabledTools.Count is 0 ? null : _enabledTools[^1].ThemeColor;
		ToolIsEnabledChanged?.Invoke(tool, isEnabled);
	}

	private TTool CreateTool<TTool>() where TTool : ToolBase, new()
	{
		var name = typeof(TTool).Name;
		var tool = new TTool { Name = name };

		tool.Initialize(new ToolControl(this, tool));

		_tools.Add(typeof(TTool), tool);
		AddChild(tool);

		Log.Debug("Created tool {Tool}", name);

		return tool;
	}

	// Raised before the spawn state in the same frame, so tools never carry over to another plot
	private void OnLocalPlotChanged(GdPlot? plot)
	{
		_spawnDisabledTools.Clear();

		// Moving onto a plot that's already spawned isn't a spawn, so it leaves nothing to restore
		_isEnteringSpawnedPlot = plot?.IsSpawned is true;
	}

	private void OnIsLocalPlotSpawnedChanged(bool? isSpawned)
	{
		if (isSpawned is true && !_isEnteringSpawnedPlot)
			_spawnDisabledTools.AddRange(_enabledTools);

		_isEnteringSpawnedPlot = false;

		if (!CanEnableTools)
		{
			DisableAll();
			Selector.Clear();

			return;
		}

		if (RestoreOnDespawn)
			foreach (var tool in _spawnDisabledTools)
				RequestEnable(tool);

		_spawnDisabledTools.Clear();
	}

	private void DisableAll(ToolBase? exception = null)
	{
		// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
		foreach (var tool in _tools.Values)
			if (!ReferenceEquals(tool, exception))
				tool.DisableInternal();
	}

	// The tools affirm on submit, so the host's rejection gets its own sound
	private static void OnActionRejected(string actionId, string reason)
	{
		if (actionId is nameof(AddInstancesAction) or nameof(RemoveInstancesAction) or nameof(SetPropertiesAction))
			ToolCommon.PlaySound(ToolCommon.DissentSound);
	}
}
