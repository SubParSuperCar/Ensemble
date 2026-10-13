using System.Globalization;
using EnsembleRoot.Common.Input;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Scripts.Adornments;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Serilog;

namespace EnsembleRoot.Tooling.Tools;

public partial class DestructTool : ToolBase
{
	public static readonly StringName ToggleAction = "tool_destruct_toggle";
	public static readonly Color Theme = Colors.Red;

	private readonly SolidHighlight _hoverHighlight = new() { Name = "Hover Highlight", Tint = Theme, Visible = false };
	private readonly Dictionary<int, SolidHighlight> _selectionHighlights = [];

	private AssetHandle? _hovered;

	public override Color ThemeColor => Theme;
	public override bool UsesSelection => true;

	public override void _Ready() => AddChild(_hoverHighlight);

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsEnabled || InputSink.IsSunk || !ToolCommon.IsTriggerPressed(@event) ||
			LocalPlot is not { } plot)
			return;

		var hoveredId = _hovered is { } hovered && IsLive(hovered) ? hovered.InstanceId : (int?)null;
		var targets = Selector.Trigger(hoveredId, Input.IsActionPressed(ToolCommon.SelectionToggleAction));

		InstanceReference[] references =
			[.. targets.Select(plot.Instances.GetInstance).OfType<GdInstance>().Select(InstanceReference.From)];

		if (references.Length is 0)
			return;

		foreach (var chunk in references.Chunk(RemoveInstancesAction.MaxCount))
			new RemoveInstancesAction(chunk).Submit();

		ToolCommon.PlaySound(ToolCommon.AffirmSound);

		Log.Verbose("Submitted removal of {Count} instance(s)", references.Length);
		SetHovered(null);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsEnabled)
			UpdateHover();
	}

	protected override void OnEnable()
	{
		OnSelectionAdded([.. Selector.InstanceIds]);
		Selector.Added += OnSelectionAdded;
		Selector.Removed += OnSelectionRemoved;
	}

	protected override void OnDisable()
	{
		Selector.Added -= OnSelectionAdded;
		Selector.Removed -= OnSelectionRemoved;
		OnSelectionRemoved([.. _selectionHighlights.Keys]);

		SetHovered(null);
	}

	private static bool IsLive(AssetHandle handle) => IsInstanceValid(handle) && !handle.IsQueuedForDeletion();

	private static void Cover(SolidHighlight highlight, AssetHandle handle)
	{
		var aabb = handle.BoundaryAabb;
		var transform = handle.GlobalTransform;
		transform.Origin = transform * aabb.GetCenter();

		highlight.Aabb = aabb;
		highlight.GlobalTransform = transform;
	}

	private void UpdateHover()
	{
		if (
			ToolCommon.CastCursorRay(GetViewport()) is { Collider: var collider } &&
			ToolCommon.FindInHierarchy<AssetHandle>(collider) is { } handle &&
			ToolCommon.IsHandleLocal(handle))
			SetHovered(handle);
		else
			SetHovered(null);
	}

	private void SetHovered(AssetHandle? handle)
	{
		if (ReferenceEquals(_hovered, handle))
			return;

		_hovered = handle;
		Log.Verbose("Hovered: {Handle}", handle?.Name);

		UpdateHoverHighlight();
	}

	// Selected instances keep their own highlights
	private void UpdateHoverHighlight()
	{
		if (_hovered is not { } handle || !IsLive(handle) || Selector.Contains(handle.InstanceId))
		{
			_hoverHighlight.Visible = false;
			return;
		}

		Cover(_hoverHighlight, handle);
		_hoverHighlight.Visible = true;
	}

	private void OnSelectionAdded(IReadOnlyList<int> instanceIds)
	{
		foreach (var id in instanceIds)
		{
			if (ToolCommon.LocalPlotHandle?.GetInstanceHandle(id) is not { } handle)
				continue;

			var name = string.Create(CultureInfo.InvariantCulture, $"Selection Highlight {id}");
			var highlight = new SolidHighlight { Name = name, Tint = Theme };

			AddChild(highlight);

			Cover(highlight, handle);
			_selectionHighlights.Add(id, highlight);
		}

		UpdateHoverHighlight();
	}

	private void OnSelectionRemoved(IReadOnlyList<int> instanceIds)
	{
		foreach (var id in instanceIds)
			if (_selectionHighlights.Remove(id, out var highlight))
				highlight.QueueFree();

		UpdateHoverHighlight();
	}
}
