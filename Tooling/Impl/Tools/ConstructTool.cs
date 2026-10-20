using System.Diagnostics;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Common.Utils;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Scripts.Adornments;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Plots;
using EnsembleRoot.Scripts.Plots.Placement;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Serilog;

// ReSharper disable EventNeverSubscribedTo.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Tooling.Tools;

public enum RotationSpace : byte
{
	Global,
	Local
}

public partial class ConstructTool : ToolBase
{
	private const float LinearSmoothingRate = 40f;
	private const float AngularSmoothingRate = 36f;

	private const float PreviewOpacity = 3 / 4f;
	private const double FlashDuration = 1 / 8d;

	public static readonly StringName ToggleAction = "tool_construct_toggle";
	public static readonly Color Theme = new("#40A0FF");

	private static readonly StringName RotateXAction = "tool_construct_rotate_x";
	private static readonly StringName RotateYAction = "tool_construct_rotate_y";
	private static readonly StringName RotateZAction = "tool_construct_rotate_z";

	private static readonly Color InvalidTint = Colors.Red;

	private readonly PlacementSolver _solver = new();

	private AxialHighlight? _axialHighlight;
	private Vector3 _gridPosition;
	private AssetHandle? _preview;
	private Aabb _previewBounds;
	private Quaternion _rotation = Quaternion.Identity;
	private SolidHighlight? _solidHighlight;
	private PlacementState? _state;
	private Transform3D _targetTransform;

	public override Color ThemeColor => Theme;

	public RotationSpace RotationSpace { get; set; } = RotationSpace.Global;

	public float? SnappingIncrementLinear { get; set; } = 1f;
	public float SnappingIncrementAngularRadians { get; set; } = MathF.PI / 2;

	public int AssetId { get; private set; }

	public bool IsActive { get; private set; }

	private bool CanPlace => _state is PlacementState.Valid;
	private bool IsSnapping => SnappingIncrementLinear is > 0;

	public event Action<int>? AssetIdChanged;
	public event Action<bool>? IsActiveChanged;

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
		else if (ToolCommon.IsTriggerPressed(@event))
			TryPlace();
	}

	public override void _Process(double delta)
	{
		if (_preview is not { Visible: true } preview)
			return;

		var current = preview.GlobalTransform;
		var linearWeight = Smoothing.GetWeight(LinearSmoothingRate, delta);
		var angularWeight = Smoothing.GetWeight(AngularSmoothingRate, delta);

		var rotation = current.Basis.GetRotationQuaternion().Normalized()
			.Slerp(_targetTransform.Basis.GetRotationQuaternion().Normalized(), angularWeight);

		preview.GlobalTransform = new Transform3D(
			new Basis(rotation) * Basis.FromScale(_targetTransform.Basis.Scale),
			current.Origin.Lerp(_targetTransform.Origin, linearWeight));
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsActive)
			UpdatePlacement();
	}

	public void RotateX() => Rotate(Vector3.Up);
	public void RotateY() => Rotate(Vector3.Right);
	public void RotateZ() => Rotate(Vector3.Back);

	public void ResetRotation()
	{
		_rotation = Quaternion.Identity;
		_state = null;
	}

	public void SetAsset(int id)
	{
		if (AssetId == id)
			return;

		AssetId = id;
		AssetIdChanged?.Invoke(id);

		var wasActive = IsActive;
		UpdateActive();

		if (wasActive && IsActive)
			RebuildPreview();
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
		if (ToolCommon.LocalPlotHandle is not { } plot || ToolCommon.CastCursorRay(GetViewport()) is not { } hit)
		{
			HidePreview();
			return;
		}

		var normal = (plot.OriginTransform.Basis.Inverse() * hit.Normal).Normalized();

		if (IsSnapping)
			normal = ToGridAxis(normal);

		var surface = plot.WorldToGrid(hit.Position) * PlotHandle.GridToWorldScale;
		var box = PlotPlacement.GetBox(AssetId, Vector3.Zero, _rotation);
		box = Settle(box.At(surface + normal * box.Radius(normal)), plot, normal, hit.Collider);

		_gridPosition = (box.Center - box.Basis * _previewBounds.GetCenter()) / PlotHandle.GridToWorldScale;
		_state = PlotPlacement.Evaluate(plot, AssetId, _gridPosition, _rotation);

		if (_state is PlacementState.OutOfBounds)
		{
			_preview!.Visible = false;
			return;
		}

		_targetTransform = plot.GridToWorld(_gridPosition, _rotation);

		if (!_preview!.Visible)
			_preview.GlobalTransform = _targetTransform;

		_preview.Visible = true;
		_axialHighlight!.Visible = CanPlace;
		_solidHighlight!.Visible = !CanPlace;
	}

	private void TryPlace()
	{
		if (CanPlace)
		{
			new AddInstanceAction(AssetId, _gridPosition, _rotation).Submit();
			ToolCommon.PlaySound(ToolCommon.AffirmSound);

			Log.Verbose(
				"Submitted placement of asset {AssetId} (Position={Position}, Rotation={Rotation})",
				AssetId, _gridPosition, _rotation);

			return;
		}

		if (_preview is { Visible: true })
		{
			ToolCommon.PlaySound(ToolCommon.DissentSound);
			Flash();
		}

		if (_state is { } state)
			Log.Debug("Cannot place asset {AssetId}: {State}", AssetId, state);
	}

	private static Vector3 ToGridAxis(Vector3 normal)
	{
		var axis = (int)normal.Abs().MaxAxisIndex();
		var gridAxis = Vector3.Zero;
		gridAxis[axis] = MathF.Sign(normal[axis]);

		return gridAxis;
	}

	// The touched face anchors the normal axis, so contact stays flush with off-grid assets
	private static Vector3 GetGridAnchor(Node3D collider, Vector3 normal, float cellSize)
	{
		if (ToolCommon.FindInHierarchy<AssetHandle>(collider) is not { } target || !ToolCommon.IsHandleLocal(target))
			return Vector3.Zero;

		var box = Obb.From(target.BoundaryAabb, target.Transform);
		var anchor = box.Center;
		var envelope = box.Envelope;

		for (var axis = 0; axis < 3; axis++)
			anchor[axis] += (normal[axis] > 0 ? 1 : -1) * PlacementSolver.GetSnapExtent(envelope[axis], cellSize);

		return anchor;
	}

	/// <summary>Settles a box resting against the hit surface (see <see cref="PlacementSolver" />).</summary>
	private Obb Settle(Obb box, PlotHandle plot, Vector3 normal, Node3D collider)
	{
		var cellSize = IsSnapping ? SnappingIncrementLinear * PlotHandle.GridToWorldScale : null;
		var anchor = cellSize is { } size ? GetGridAnchor(collider, normal, size) : Vector3.Zero;

		return _solver.Settle(
			box, PlotPlacement.GetObstacles(plot), PlotPlacement.GetBounds(plot).Position.Y, normal, cellSize, anchor);
	}

	private void RebuildPreview()
	{
		DestroyPreview();

		if (GAssetManager.GetPackedOrNull(AssetId) is not { } packed)
			return;

		_preview = packed.Instantiate<AssetHandle>();
		_preview.Name = "Preview";
		_preview.SetCollision(0, 0);
		_preview.InputRayPickable = false;
		_preview.Visible = false;

		AddChild(_preview);
		MakeTranslucent(_preview);

		_previewBounds = _preview.BoundaryAabb;
		_axialHighlight = new AxialHighlight { Name = "Valid Highlight", Aabb = _previewBounds };
		_solidHighlight = new SolidHighlight { Name = "Invalid Highlight", Aabb = _previewBounds, Tint = InvalidTint };

		_preview.AddChild(_axialHighlight);
		_preview.AddChild(_solidHighlight);
	}

	// GeometryInstance3D.Transparency is ignored outside Forward+
	private static void MakeTranslucent(Node root)
	{
		foreach (var node in root.FindChildren("*", nameof(MeshInstance3D), true, false))
		{
			var mesh = (MeshInstance3D)node;

			for (var surface = 0; surface < mesh.GetSurfaceOverrideMaterialCount(); surface++)
			{
				if (mesh.GetActiveMaterial(surface)?.Duplicate() is not BaseMaterial3D material)
					continue;

				material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
				material.AlbedoColor = material.AlbedoColor with { A = material.AlbedoColor.A * PreviewOpacity };
				mesh.SetSurfaceOverrideMaterial(surface, material);
			}
		}
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
		_solidHighlight!.CreateTween()
			.TweenProperty(_solidHighlight, nameof(SolidHighlight.Tint), InvalidTint, FlashDuration)
			.From(Colors.White);

	private void Rotate(Vector3 axis)
	{
		var increment = new Quaternion(axis, SnappingIncrementAngularRadians);

		_rotation = (RotationSpace switch
		{
			RotationSpace.Global => increment * _rotation,
			RotationSpace.Local => _rotation * increment,
			_ => throw new UnreachableException()
		}).Normalized();

		// The last evaluation was for the old rotation, so placing waits for the next one
		_state = null;
	}
}
