using EnsembleRoot.Common.Input;
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

	private readonly SolidHighlight _highlight = new() { Name = "Selection Highlight", Tint = Theme, Visible = false };

	private AssetHandle? _selected;

	public override Color ThemeColor => Theme;

	public override void _Ready() => AddChild(_highlight);

	public override void _UnhandledInput(InputEvent @event)
	{
		if (
			!IsEnabled || InputSink.IsSunk ||
			_selected is not { } selected || !IsInstanceValid(selected) || selected.IsQueuedForDeletion() ||
			!@event.IsActionPressed(ToolCommon.TriggerAction) ||
			LocalPlot?.Instances.GetInstance(selected.InstanceId) is not { } instance)
			return;

		new RemoveInstanceAction(InstanceReference.From(instance)).Submit();
		ToolCommon.PlaySound(ToolCommon.AffirmSound);

		Log.Verbose("Submitted removal of instance {InstanceId}", instance.Id);
		SetSelected(null);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsEnabled)
			UpdateSelection();
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
