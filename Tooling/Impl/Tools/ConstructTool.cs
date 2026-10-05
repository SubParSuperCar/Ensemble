using System.Diagnostics;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Scripts.Adornments;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Plots;
using EnsembleRoot.Scripts.Plots.Impl;
using EnsembleRoot.SessionManager.Actions;
using Godot;
using Serilog;

namespace EnsembleRoot.Tooling.Tools;

public enum RotationSpace : byte
{
	Global,
	Local
}

public partial class ConstructTool : ToolBase
{
	private static readonly StringName RotateXAction = "tool_ctor_rot_x";
	private static readonly StringName RotateYAction = "tool_ctor_rot_y";
	private static readonly StringName RotateZAction = "tool_ctor_rot_z";

	private AxialHighlight? _axialHighlight;
	private Vector3 _gridPosition;
	private AssetHandle? _preview;
	private Aabb _previewBounds;
	private Quaternion _rotation = Quaternion.Identity;
	private SolidHighlight? _solidHighlight;
	private PlacementState? _state;

	protected override StringName ToggleAction => "tool_construct_toggle";

	public RotationSpace RotationSpace { get; set; } = RotationSpace.Global;

	public float? SnappingIncrementLinear { get; set; } = 1f;
	public float SnappingIncrementAngularRadians { get; set; } = MathF.PI / 2;

	public int AssetId { get; private set; }

	public bool IsActive { get; private set; }

	private bool CanPlace => _state is PlacementState.Valid;
	private bool IsSnapping => SnappingIncrementLinear is > 0;

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

	public void ResetRotation() => _rotation = Quaternion.Identity;

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

		var normal = (plot.OriginTransform.Basis.Inverse() * hit.Normal).Normalized();

		if (IsSnapping)
			normal = ToGridAxis(normal);

		var surface = plot.WorldToGrid(hit.Position) * PlotHandle.GridToWorldScale;

		var box = PlotPlacement.GetBox(AssetId, Vector3.Zero, _rotation);
		box = Settle(box.At(surface + normal * box.Radius(normal)), plot, normal, GetGridAnchor(hit.Collider));

		_gridPosition = (box.Center - box.Basis * _previewBounds.GetCenter()) / PlotHandle.GridToWorldScale;
		_state = PlotPlacement.Evaluate(plot, AssetId, _gridPosition, _rotation);

		if (_state is PlacementState.OutOfBounds)
		{
			_preview!.Visible = false;
			return;
		}

		_preview!.GlobalTransform = plot.GridToWorld(_gridPosition, _rotation);
		_preview.Visible = true;
		_axialHighlight!.Visible = CanPlace;
		_solidHighlight!.Visible = !CanPlace;
	}

	private void TryPlace()
	{
		if (CanPlace)
		{
			new AddInstanceAction(AssetId, _gridPosition, _rotation).Submit();
			ToolCommon.PlaySound("affirm");

			Log.Verbose(
				"Submitted asset id: {AssetId} (Position={Position}, Rotation={Rotation})",
				AssetId,
				_gridPosition,
				_rotation);

			return;
		}

		if (_preview is { Visible: true })
		{
			ToolCommon.PlaySound("dissent");
			Flash();
		}

		if (_state is { } state)
			Log.Debug("Cannot place asset id {AssetId}: {State}", AssetId, state);
	}

	// Snapped placements rest against the nearest face of the surface's bounds, matching box-based collision
	private static Vector3 ToGridAxis(Vector3 normal)
	{
		var axis = (int)normal.Abs().MaxAxisIndex();
		var gridAxis = Vector3.Zero;
		gridAxis[axis] = MathF.Sign(normal[axis]);

		return gridAxis;
	}

	// Snapping is relative to the targeted instance's corner, so edges line up with it at any increment
	private static Vector3 GetGridAnchor(Node3D collider)
	{
		if (ToolCommon.FindInHierarchy<AssetHandle>(collider) is not { } target || !ToolCommon.IsHandleLocal(target))
			return Vector3.Zero;

		var box = Obb.From(target.BoundaryAabb, target.Transform);
		return box.Center - box.Envelope;
	}

	private Obb Settle(Obb box, PlotHandle plot, Vector3 normal, Vector3 anchor)
	{
		var obstacles = PlotPlacement.GetObstacles(plot).ToArray();
		var floor = PlotPlacement.GetBounds(plot).Position.Y;
		var cellSize = IsSnapping ? SnappingIncrementLinear * PlotHandle.GridToWorldScale : null;

		box = Snap(box, cellSize, anchor, normal, Vector3.Zero);

		for (var pass = 0; pass <= obstacles.Length; pass++)
		{
			var resolved = Resolve(box, obstacles, normal);
			resolved = Snap(resolved, cellSize, anchor, normal, resolved.Center - box.Center);

			box = RaiseTo(resolved, floor);

			if (!obstacles.Any(box.Overlaps))
				break;
		}

		return box;
	}

	private static Obb Resolve(Obb box, Obb[] obstacles, Vector3 normal)
	{
		for (var pass = 0; pass <= obstacles.Length; pass++)
		{
			var isMoved = false;

			foreach (var obstacle in obstacles)
			{
				if (!box.Overlaps(obstacle))
					continue;

				var translation = box.GetMinimumTranslation(obstacle, normal);

				if (translation == Vector3.Zero)
					continue;

				box = box.At(box.Center + translation);
				isMoved = true;
			}

			if (!isMoved)
				break;
		}

		return box;
	}

	// Snaps along the surface only, aligning whichever edge keeps the box nearest, never rounding back against a push
	private static Obb Snap(Obb box, float? cellSize, Vector3 anchor, Vector3 normal, Vector3 push)
	{
		if (cellSize is not { } size)
			return box;

		var center = box.Center;
		var envelope = box.Envelope;

		for (var axis = 0; axis < 3; axis++)
		{
			if (!Mathf.IsZeroApprox(normal[axis]))
				continue;

			var direction = Mathf.IsZeroApprox(push[axis]) ? 0 : MathF.Sign(push[axis]);
			var low = SnapEdge(center[axis] - envelope[axis], anchor[axis], size, direction) + envelope[axis];
			var high = SnapEdge(center[axis] + envelope[axis], anchor[axis], size, direction) - envelope[axis];

			center[axis] = MathF.Abs(low - center[axis]) <= MathF.Abs(high - center[axis]) ? low : high;
		}

		return box.At(center);
	}

	private static float SnapEdge(float edge, float anchor, float cellSize, int direction)
	{
		var cells = (edge - anchor) / cellSize;
		var nearest = MathF.Round(cells, MidpointRounding.AwayFromZero);

		var snapped = Mathf.IsEqualApprox(cells, nearest) || direction is 0
			? nearest
			: direction > 0
				? MathF.Ceiling(cells)
				: MathF.Floor(cells);

		return anchor + snapped * cellSize;
	}

	private static Obb RaiseTo(Obb box, float floor)
	{
		var depth = floor - (box.Center.Y - box.Envelope.Y);
		return depth > 0 && !Mathf.IsZeroApprox(depth) ? box.At(box.Center + Vector3.Up * depth) : box;
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

		_previewBounds = _preview.BoundaryAabb;

		_axialHighlight = new AxialHighlight { Name = "Valid Highlight", Aabb = _previewBounds };
		_solidHighlight = new SolidHighlight { Name = "Invalid Highlight", Aabb = _previewBounds, Tint = Colors.Red };
		_preview.AddChild(_axialHighlight);
		_preview.AddChild(_solidHighlight);
	}

	private void DestroyPreview()
	{
		_preview?.QueueFree();
		_preview = null;
		_previewBounds = default;
		_axialHighlight = null;
		_solidHighlight = null;
		_state = null;
	}

	private void HidePreview()
	{
		_preview?.Visible = false;
		_state = null;
	}

	private void Flash() =>
		CreateTween().TweenProperty(_solidHighlight!, "Tint", Colors.Red, 1 / 8f).From(Colors.White);

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
}
