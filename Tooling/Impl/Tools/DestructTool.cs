using EnsembleRoot.Common.Input;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Scripts.Adornments;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.SessionManager.Actions;
using Godot;
using Serilog;

namespace EnsembleRoot.Tooling.Tools;

public partial class DestructTool : ToolBase
{
	private static readonly StringName ToggleActionName = "tool_destruct_toggle";

	private readonly SolidHighlight _highlight = new() { Tint = Colors.Red };
	private AssetHandle? _selected;

	protected override StringName ToggleAction => ToggleActionName;

	public override void _Ready()
	{
		_highlight.Name = "Selection Highlight";
		_highlight.Visible = false;

		AddChild(_highlight);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsEnabled)
			UpdateSelection();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (
			!IsEnabled || InputSink.IsSunk || _selected is null ||
			!@event.IsActionPressed(ToolCommon.TriggerAction) ||
			LocalPlot?.Instances.GetInstance(_selected.InstanceId) is not { } instance)
			return;

		new RemoveInstanceAction(InstanceReference.From(instance)).Submit();
		ToolCommon.PlaySound("affirm");

		Log.Verbose("Submitted removal: {InstanceId}", _selected.InstanceId);
		SetSelected(null);
	}

	protected override void OnDisable() => SetSelected(null);

	private void UpdateSelection()
	{
		if (
			ToolCommon.CastCursorRay(GetViewport()) is { Collider: var collider } &&
			ToolCommon.FindInHierarchy<AssetHandle>(collider) is { } handle &&
			ToolCommon.IsHandleLocal(handle))
			SetSelected(handle);
		else
			SetSelected(null);
	}

	private void SetSelected(AssetHandle? handle)
	{
		if (ReferenceEquals(_selected, handle))
			return;

		_selected = handle;
		Log.Verbose("Selected: {Handle}", handle?.Name);

		if (handle is null)
		{
			_highlight.Visible = false;
			return;
		}

		var aabb = handle.BoundaryAabb;
		var transform = handle.GlobalTransform;
		transform.Origin = transform * aabb.GetCenter();

		_highlight.Aabb = aabb;
		_highlight.GlobalTransform = transform;
		_highlight.Visible = true;
	}
}
