using Godot;
using Root.Common.Input;
using Root.Scripts.Adornments;
using Root.Scripts.Assets;
using Root.Scripts.Plots;
using Serilog;

namespace Root.Tooling;

public partial class DestructTool : ToolBase
{
	private const float RayLength = 1000;

	private static readonly StringName TriggerAction = "tool_trigger";

	private readonly AxialHighlight _highlight = new();
	private AssetHandle? _selected;

	protected override StringName ToggleAction => "tool_destruct_toggle";

	public override void _Ready()
	{
		_highlight.Visible = false;
		AddChild(_highlight);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsEnabled)
			OnPhysicsProcess();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsEnabled || InputSink.IsSunk || !@event.IsActionPressed(TriggerAction))
			return;

		if (_selected is null || LocalPlot?.Instances is not { } instances)
			return;

		instances.Remove(_selected.InstanceId);
		Log.Verbose("Removed: {InstanceId}", _selected.InstanceId);

		SetSelected(null);
	}

	protected override void OnDisable() => SetSelected(null);

	private void OnPhysicsProcess()
	{
		if (
			CastRay() is { } result &&
			FindInHierarchy<AssetHandle>(result) is { } handle &&
			IsHandleLocal(handle))
			SetSelected(handle);
		else
			SetSelected(null);
	}

	private Node3D? CastRay()
	{
		var viewport = GetViewport();
		var mousePosition = viewport.GetMousePosition();

		var camera = viewport.GetCamera3D();
		if (camera is null)
			return null;

		var rayOrigin = camera.ProjectRayOrigin(mousePosition);
		var rayEnd = rayOrigin + camera.ProjectRayNormal(mousePosition) * RayLength;

		var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
		query.CollideWithBodies = true;

		var result = viewport.GetWorld3D().DirectSpaceState.IntersectRay(query);
		return result.Count > 0 ? result["collider"].As<Node3D>() : null;
	}

	private static TNode? FindInHierarchy<TNode>(Node node) where TNode : Node
	{
		while (node is not null)
		{
			if (node is TNode ofType)
				return ofType;

			node = node.GetParent();
		}

		return null;
	}

	private static bool IsHandleLocal(AssetHandle handle)
	{
		if (LocalPlot?.Id is not { } id)
			return false;

		var plotHandle = FindInHierarchy<PlotHandle>(handle);
		return ReferenceEquals(GPlotManager.GetHandle(id), plotHandle);
	}

	private void SetSelected(AssetHandle? handle)
	{
		if (ReferenceEquals(_selected, handle))
			return;

		_selected = handle;
		Log.Verbose("Selected: {Handle}", handle?.Name);

		if (handle is null)
		{
			_highlight.Reparent(this);
			_highlight.Visible = false;
			return;
		}

		_highlight.Reparent(handle);
		_highlight.Aabb = handle.BoundaryAabb;
		_highlight.Visible = true;
	}
}
