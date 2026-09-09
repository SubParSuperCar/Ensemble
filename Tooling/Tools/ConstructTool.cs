using System.Diagnostics;
using Godot;
using Root.Common.Input;
using Root.Scripts.Adornments;
using Root.Scripts.Assets;
using Root.Scripts.Plots;
using Serilog;

namespace Root.Tooling.Tools;

public enum RotationSpace : byte
{
	Global,
	Local
}

internal enum PlacementState : byte
{
	Valid,
	Overlapping,
	LimitReached
}

// TODO: W.I.P.
public partial class ConstructTool : ToolBase
{
	private const float OverlapProbeInset = 0.02f;

	private static readonly StringName RotateXAction = "tool_ctor_rot_x";
	private static readonly StringName RotateYAction = "tool_ctor_rot_y";
	private static readonly StringName RotateZAction = "tool_ctor_rot_z";

	private AxialHighlight? _axialHighlight;
	private bool _canPlace;
	private Vector3 _gridPosition;
	private AssetHandle? _preview;
	private Aabb _previewBounds;
	private Shape3D? _previewShape;
	private Quaternion _rotation = Quaternion.Identity;
	private SolidHighlight? _solidHighlight;

	protected override StringName ToggleAction => "tool_construct_toggle";

	public RotationSpace RotationSpace { get; set; } = RotationSpace.Global;

	public float? SnappingIncrementLinear { get; set; } = 1;
	public float SnappingIncrementAngularRadians { get; set; } = MathF.PI / 2;

	public int AssetId { get; private set; } = 2;

	public bool IsActive { get; private set; }
	public event Action<int>? AssetIdChanged;
	public event Action<bool>? IsActiveChanged;

	public override void _PhysicsProcess(double delta)
	{
		if (IsActive)
			UpdatePlacement();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsActive || InputSink.IsSunk)
			return;

		if (@event.IsActionPressed(RotateXAction))
			RotateX();
		else if (@event.IsActionPressed(RotateYAction))
			RotateY();
		else if (@event.IsActionPressed(RotateZAction))
			RotateZ();
		else if (@event.IsActionPressed(ToolCommon.TriggerAction))
			TryPlace();
	}

	public void RotateX() => Rotate(Vector3.Up);
	public void RotateY() => Rotate(Vector3.Right);
	public void RotateZ() => Rotate(Vector3.Back);

	public void SetAsset(int id)
	{
		if (AssetId == id)
			return;

		AssetId = id;
		AssetIdChanged?.Invoke(id);

		if (IsActive)
			RebuildPreview();
		else
			UpdateActive();
	}

	protected override void OnEnable() => UpdateActive();
	protected override void OnDisable() => UpdateActive();

	private void UpdateActive()
	{
		var isActive = IsEnabled && GAssetManager.GetPackedOrNull(AssetId) is not null;

		if (isActive == IsActive)
			return;

		IsActive = isActive;
		IsActiveChanged?.Invoke(isActive);

		if (isActive)
			RebuildPreview();
		else
			DestroyPreview();
	}

	private void UpdatePlacement()
	{
		if (
			ToolCommon.LocalPlotHandle is not { } plot ||
			ToolCommon.CastCursorRay(GetViewport()) is not { } hit)
		{
			HidePreview();
			return;
		}

		var extents = GridExtents();
		var normal = (plot.OriginTransform.Basis.Inverse() * hit.Normal).Normalized();
		var target = plot.WorldToGrid(hit.Position) + normal * extents.Dot(normal.Abs());
		var position = SnapToGrid(target, extents);

		if (!IsWithin(GridBounds(plot), position, extents))
		{
			HidePreview();
			return;
		}

		_gridPosition = position;
		_preview!.GlobalTransform = plot.GridToWorld(position, _rotation);

		_canPlace = EvaluateState(plot, position) is PlacementState.Valid;
		_preview.Visible = true;
		_axialHighlight!.Visible = _canPlace;
		_solidHighlight!.Visible = !_canPlace;
	}

	private void TryPlace()
	{
		if (_canPlace)
		{
			LocalPlot?.Instances.Add(AssetId, _gridPosition, _rotation);

			Log.Verbose("Added: {AssetId} (Position={Position}, Rotation={Rotation}",
				AssetId, _gridPosition, _rotation);
		}
		else if (_preview is { Visible: true })
			Flash();
	}

	private Vector3 SnapToGrid(Vector3 position, Vector3 extents)
	{
		if (SnappingIncrementLinear is { } increment and > 0)
		{
			var min = position - extents;
			min = min.Snapped(Vector3.One * increment);
			position = min + extents;
		}

		position.Y = MathF.Max(position.Y, extents.Y);
		return position;
	}

	private Vector3 GridExtents()
	{
		var basis = new Basis(_rotation);
		var local = _previewBounds.Size / (2 * PlotHandle.GridToWorldScale);

		return basis.X.Abs() * local.X + basis.Y.Abs() * local.Y + basis.Z.Abs() * local.Z;
	}

	private bool IntersectsInstance(PlotHandle plot, Vector3 position)
	{
		var probe = plot.GridToWorld(position, _rotation);
		probe.Basis = probe.Basis.Scaled(Vector3.One * (1 - OverlapProbeInset));

		var query = new PhysicsShapeQueryParameters3D
		{
			Shape = _previewShape,
			Transform = probe,
			CollisionMask = ToolCommon.SelectableLayers,
			CollideWithBodies = true,
			CollideWithAreas = false,
			Exclude = [_preview!.GetRid()]
		};

		var hits = GetViewport().GetWorld3D().DirectSpaceState.IntersectShape(query);

		foreach (var hit in hits)
			if (ToolCommon.FindInHierarchy<AssetHandle>(hit["collider"].As<Node3D>()) is not null)
				return true;

		return false;
	}

	private PlacementState EvaluateState(PlotHandle plot, Vector3 position)
	{
		if (IntersectsInstance(plot, position) || LocalPlot?.Instances is not { } instances)
			return PlacementState.Overlapping;

		var counts = instances.GetCount(AssetId);
		var placed = counts[0];
		var limit = counts[1];

		if (
			(limit is not Unlimited && placed >= limit) ||
			(instances.MaxCount is not Unlimited && instances.Count >= instances.MaxCount))
			return PlacementState.LimitReached;

		return PlacementState.Valid;
	}

	private void RebuildPreview()
	{
		DestroyPreview();

		if (GAssetManager.GetPackedOrNull(AssetId) is not { } packed)
			return;

		_preview = packed.Instantiate<AssetHandle>();
		_preview.Name = "Preview";
		_preview.Freeze = true;
		_preview.CollisionLayer = 0;
		_preview.CollisionMask = 0;
		_preview.InputRayPickable = false;
		_preview.Visible = false;
		AddChild(_preview);

		_previewShape = _preview.GetNode<CollisionShape3D>("Collider").Shape;
		_previewBounds = _preview.BoundaryAabb;

		_axialHighlight = new AxialHighlight { Aabb = _previewBounds };
		_solidHighlight = new SolidHighlight { Aabb = _previewBounds, Tint = Colors.Red };
		_preview.AddChild(_axialHighlight);
		_preview.AddChild(_solidHighlight);
	}

	private void DestroyPreview()
	{
		_preview?.QueueFree();
		_preview = null;
		_previewShape = null;
		_previewBounds = default;
		_axialHighlight = null;
		_solidHighlight = null;
		_canPlace = false;
	}

	private void HidePreview()
	{
		_preview?.Visible = false;
		_canPlace = false;
	}

	private void Flash() => CreateTween().TweenProperty(_solidHighlight!, "Tint", Colors.Red, 0.15).From(Colors.White);

	private void Rotate(Vector3 axis)
	{
		var increment = new Quaternion(axis, SnappingIncrementAngularRadians);

		_rotation = (RotationSpace switch
		{
			RotationSpace.Global => increment * _rotation,
			RotationSpace.Local => _rotation * increment,
			_ => throw new UnreachableException()
		}).Normalized();
	}

	private static Aabb GridBounds(PlotHandle plot)
	{
		var min = plot.WorldToGrid(plot.BoundaryTransform.Origin) - plot.GridBoundarySize / 2;
		min.Y = MathF.Max(min.Y, 0);

		return new Aabb(min, plot.GridBoundarySize);
	}

	private static bool IsWithin(Aabb bounds, Vector3 position, Vector3 extents)
	{
		const float epsilon = 1e-3f;

		var min = position - extents;
		var max = position + extents;

		return
			min.X >= bounds.Position.X - epsilon &&
			min.Y >= bounds.Position.Y - epsilon &&
			min.Z >= bounds.Position.Z - epsilon &&
			max.X <= bounds.End.X + epsilon &&
			max.Y <= bounds.End.Y + epsilon &&
			max.Z <= bounds.End.Z + epsilon;
	}
}
