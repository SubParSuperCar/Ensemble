using Godot;
using Root.Scripts.Assets;
using Root.Scripts.Plots;

namespace Root.Tooling.Tools;

internal readonly record struct ToolRayHit(Vector3 Position, Vector3 Normal, Node3D Collider);

internal static class ToolCommon
{
	private const float RayLength = 1000;
	public const uint SelectableLayers = 1;

	public static readonly StringName TriggerAction = "tool_trigger";

	public static PlotHandle? LocalPlotHandle => LocalPlot?.Id is { } id ? GPlotManager.GetHandleOrNull(id) : null;

	public static TNode? FindInHierarchy<TNode>(Node? node) where TNode : Node
	{
		for (var current = node; current is not null; current = current.GetParent())
			if (current is TNode match)
				return match;

		return null;
	}

	public static bool IsHandleLocal(AssetHandle handle) =>
		LocalPlotHandle is { } plot && ReferenceEquals(FindInHierarchy<PlotHandle>(handle), plot);

	public static ToolRayHit? CastCursorRay(Viewport viewport, uint mask = SelectableLayers)
	{
		if (viewport.GetCamera3D() is not { } camera)
			return null;

		var mouse = viewport.GetMousePosition();
		var origin = camera.ProjectRayOrigin(mouse);
		var end = origin + camera.ProjectRayNormal(mouse) * RayLength;

		var query = PhysicsRayQueryParameters3D.Create(origin, end, mask);
		query.CollideWithBodies = true;
		query.CollideWithAreas = false;

		var result = viewport.GetWorld3D().DirectSpaceState.IntersectRay(query);

		return result.Count > 0
			? new ToolRayHit(
				result["position"].AsVector3(),
				result["normal"].AsVector3(),
				result["collider"].As<Node3D>())
			: null;
	}
}
