using EnsembleRoot.Common.Input;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Plots;
using Godot;

namespace EnsembleRoot.Tooling.Tools;

internal readonly record struct ToolRayHit(Vector3 Position, Vector3 Normal, Node3D Collider);

internal static class ToolCommon
{
	public const string AffirmSound = "affirm";
	public const string DissentSound = "dissent";

	private const uint SelectableLayers = 1;
	private const float RayLength = 1000f;
	private const string SoundBus = "master";

	public static readonly StringName TriggerAction = "tool_trigger";

	private static readonly NodePath SoundManagerPath = "SoundManager";
	private static readonly StringName PlayMethod = "play";

	public static PlotHandle? LocalPlotHandle => LocalPlot?.Id is { } id ? GPlotManager.GetHandleOrNull(id) : null;

	private static Node SoundManager => field ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode(SoundManagerPath);

	public static TNode? FindInHierarchy<TNode>(Node? node) where TNode : Node
	{
		for (var current = node; current is not null; current = current.GetParent())
			if (current is TNode match)
				return match;

		return null;
	}

	public static void PlaySound(string name) => SoundManager.Call(PlayMethod, SoundBus, name);

	public static bool IsHandleLocal(AssetHandle handle) =>
		LocalPlotHandle is { } plot && ReferenceEquals(FindInHierarchy<PlotHandle>(handle), plot);

	public static ToolRayHit? CastCursorRay(Viewport viewport, uint mask = SelectableLayers)
	{
		if (viewport.GetCamera3D() is not { } camera)
			return null;

		var mouse = Pointer.GetPosition(viewport);
		var origin = camera.ProjectRayOrigin(mouse);
		var end = origin + camera.ProjectRayNormal(mouse) * RayLength;

		var query = PhysicsRayQueryParameters3D.Create(origin, end, mask);
		var result = viewport.GetWorld3D().DirectSpaceState.IntersectRay(query);

		return result.Count is 0
			? null
			: new ToolRayHit(
				result["position"].AsVector3(), result["normal"].AsVector3(), result["collider"].As<Node3D>());
	}
}
