using EnsembleRoot.Common.Input;
using EnsembleRoot.Scripts.Assets;
using Godot;

namespace EnsembleRoot.Tooling.Tools;

internal readonly record struct ToolRayHit(Vector3 Position, Vector3 Normal, Node3D Collider);

internal static class ToolCommon
{
	public const string AffirmSound = "affirm";
	public const string DissentSound = "dissent";

	private const uint SelectableLayers = PhysicsLayers.World | PhysicsLayers.StaticInstances;
	private const string SoundBus = "master";

	public static readonly StringName TriggerAction = "tool_trigger";
	public static readonly StringName SelectionMarqueeAction = "tool_selection_marquee";
	public static readonly StringName SelectionToggleAction = "tool_selection_toggle";

	private static readonly NodePath SoundManagerPath = "SoundManager";
	private static readonly StringName PlayMethod = "play";

	private static Node SoundManager => field ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode(SoundManagerPath);

	public static TNode? FindInHierarchy<TNode>(Node? node) where TNode : Node
	{
		for (var current = node; current is not null; current = current.GetParent())
			if (current is TNode match)
				return match;

		return null;
	}

	public static void PlaySound(string name) => SoundManager.Call(PlayMethod, SoundBus, name);

	// Presses with the marquee key held start a marquee instead, if any enabled tool selects
	public static bool IsTriggerPressed(InputEvent @event) =>
		@event.IsActionPressed(TriggerAction) &&
		(!Input.IsActionPressed(SelectionMarqueeAction) || !GToolManager.Marquee.CanSelect);

	public static bool IsHandleLocal(AssetHandle handle) =>
		GPlotManager.LocalHandle is { } plot && ReferenceEquals(handle.Plot, plot);

	public static ToolRayHit? CastCursorRay(Viewport viewport)
	{
		if (viewport.GetCamera3D() is not { } camera)
			return null;

		var mouse = Pointer.GetPosition(viewport);
		var origin = camera.ProjectRayOrigin(mouse);
		// As far as the camera draws, so anything shown can be targeted
		var end = origin + camera.ProjectRayNormal(mouse) * camera.Far;

		var query = PhysicsRayQueryParameters3D.Create(origin, end, SelectableLayers);
		var result = viewport.GetWorld3D().DirectSpaceState.IntersectRay(query);

		return result.Count is 0
			? null
			: new ToolRayHit(
				result["position"].AsVector3(), result["normal"].AsVector3(), result["collider"].As<Node3D>());
	}

	/// <summary>Finds the local plot's instances whose boundary centers project within a viewport area.</summary>
	public static List<int> FindInstancesInArea(Viewport viewport, Rect2 area)
	{
		if (
			viewport.GetCamera3D() is not { } camera || LocalPlot is not { } plot ||
			GPlotManager.LocalHandle is not { } handle)
			return [];

		var instanceIds = new List<int>();

		foreach (var instance in plot.Instances.Source.All)
		{
			if (handle.GetInstanceHandle(instance.Id) is not { } asset)
				continue;

			var center = asset.GlobalTransform * GAssetManager.GetBoundary(asset.AssetId).GetCenter();

			if (!camera.IsPositionBehind(center) && area.HasPoint(camera.UnprojectPosition(center)))
				instanceIds.Add(instance.Id);
		}

		return instanceIds;
	}
}
