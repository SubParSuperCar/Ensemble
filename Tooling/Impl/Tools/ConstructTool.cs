using System.Diagnostics;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Common.Utils;
using EnsembleRoot.Scripts.Adornments;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Plots;
using EnsembleRoot.Scripts.Plots.Placement;
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
	private const float LinearSmoothingRate = 40f;
	private const float AngularSmoothingRate = 36f;

	private const float PreviewOpacity = 3 / 4f;
	private const double FlashDuration = 1 / 8d;

	public static readonly StringName ToggleAction = "tool_construct_toggle";
	public static readonly Color Theme = new("#40A0FF");

	private static readonly StringName RotateXAction = "tool_construct_rotate_x";
	private static readonly StringName RotateYAction = "tool_construct_rotate_y";
	private static readonly StringName RotateZAction = "tool_construct_rotate_z";

	private static readonly StringName TranslucentShaderMeta = "translucent_shader";
	private static readonly StringName OpacityParameter = "opacity";

	private static readonly Color InvalidTint = Colors.Red;

	private readonly PlacementSolver _solver = new();

	private AxialHighlight? _axialHighlight;
	private Vector3 _gridPosition;
	private bool _isActive;
	private AssetHandle? _preview;
	private Quaternion _rotation = Quaternion.Identity;
	private SolidHighlight? _solidHighlight;
	private PlacementState? _state;
	private Transform3D _targetTransform;

	public override Color ThemeColor => Theme;

	public RotationSpace RotationSpace { get; set; } = RotationSpace.Global;

	public float? SnappingIncrementLinear { get; set; } = 1f;
	public float SnappingIncrementAngularRadians { get; set; } = MathF.PI / 2;

	public int AssetId { get; private set; }

	private bool CanPlace => _state is PlacementState.Valid;
	private bool IsSnapping => SnappingIncrementLinear is > 0;

	// ReSharper disable once EventNeverSubscribedTo.Global
	public event Action<int>? AssetIdChanged;

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_isActive || InputSink.IsSunk)
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
		if (_isActive)
			UpdatePlacement();
	}

	public void RotateX() => Rotate(Vector3.Up);
	public void RotateY() => Rotate(Vector3.Right);
	public void RotateZ() => Rotate(Vector3.Back);

	public void ResetRotation() => SetRotation(Quaternion.Identity);

	public void SetRotation(Quaternion rotation)
	{
		_rotation = rotation.Normalized();

		// The last evaluation was for the old rotation, so placing waits for the next one
		_state = null;
	}

	public void SetAsset(int id)
	{
		if (AssetId == id)
			return;

		AssetId = id;
		AssetIdChanged?.Invoke(id);

		var wasActive = _isActive;
		UpdateActive();

		if (wasActive && _isActive)
			RebuildPreview();
	}

	protected override void OnEnable() => UpdateActive();
	protected override void OnDisable() => UpdateActive();

	private void UpdateActive()
	{
		var isActive = IsEnabled && GAssetManager.GetPackedOrNull(AssetId) is not null;

		if (isActive == _isActive)
			return;

		_isActive = isActive;

		if (isActive)
			RebuildPreview();
		else
			DestroyPreview();
	}

	private void UpdatePlacement()
	{
		if (GPlotManager.LocalHandle is not { } plot || ToolCommon.CastCursorRay(GetViewport()) is not { } hit)
		{
			HidePreview();
			return;
		}

		var toPlot = plot.OriginTransform.AffineInverse();
		var normal = (toPlot.Basis * hit.Normal).Normalized();

		if (IsSnapping)
			normal = ToGridAxis(normal);

		var surface = toPlot * hit.Position;
		var box = PlotPlacement.GetBox(AssetId, Vector3.Zero, _rotation);
		var cellSize = IsSnapping ? SnappingIncrementLinear * PlotHandle.GridToWorldScale : null;
		var anchor = cellSize is { } size ? GetGridAnchor(hit.Collider, normal, size) : Vector3.Zero;

		box = _solver.Settle(
			box.At(surface + normal * box.Radius(normal)), PlotPlacement.GetObstacles(plot),
			PlotPlacement.GetBounds(plot).Position.Y, normal, cellSize, anchor);

		var origin = box.Center - box.Basis * GAssetManager.GetBoundary(AssetId).GetCenter();

		if (cellSize is { } cell)
			origin = PlacementSolver.SnapToLattice(origin, anchor, GetLatticeCell(cell));

		_gridPosition = origin / PlotHandle.GridToWorldScale;
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
			UndoRedo.Add(AssetId, _gridPosition, _rotation);
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

		var box = Obb.From(GAssetManager.GetBoundary(target.AssetId), target.Transform);
		var anchor = box.Center;
		var envelope = box.Envelope;

		for (var axis = 0; axis < 3; axis++)
			anchor[axis] += (normal[axis] > 0 ? 1 : -1) * PlacementSolver.GetSnapExtent(envelope[axis], cellSize);

		// The face is the collider's, so it's rounded onto the target's own lattice, as the target was placed
		return PlacementSolver.SnapToLattice(anchor, target.Transform.Origin, GetLatticeCell(cellSize));
	}

	// The finer of the snapping cell and the plot's, which assets are built on
	private static float GetLatticeCell(float cellSize) => MathF.Min(cellSize, PlotHandle.GridToWorldScale);

	private void RebuildPreview()
	{
		DestroyPreview();

		if (GAssetManager.GetPackedOrNull(AssetId) is not { } packed)
			return;

		_preview = packed.Instantiate<AssetHandle>();
		_preview.Name = "Preview";
		_preview.SetCollision(0, 0);
		_preview.InputRayPickable = false;
		_preview.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
		_preview.Visible = false;

		AddChild(_preview);
		MakeTranslucent(_preview);

		var bounds = GAssetManager.GetBoundary(AssetId);
		_axialHighlight = new AxialHighlight { Name = "Valid Highlight", Aabb = bounds };
		_solidHighlight = new SolidHighlight { Name = "Invalid Highlight", Aabb = bounds, Tint = InvalidTint };

		_preview.AddChild(_axialHighlight);
		_preview.AddChild(_solidHighlight);
	}

	// GeometryInstance3D.Transparency is ignored outside Forward+. Surfaces without a material draw with the default
	// one, so they get a new one to fade. Shader materials fade only through a translucent variant of their shader,
	// named by their metadata, with an opacity uniform.
	private static void MakeTranslucent(Node root)
	{
		foreach (var node in root.FindChildren("*", nameof(MeshInstance3D), true, false))
		{
			var mesh = (MeshInstance3D)node;

			for (var surface = 0; surface < mesh.GetSurfaceOverrideMaterialCount(); surface++)
				if (MakeTranslucent(mesh.GetActiveMaterial(surface)) is { } material)
					mesh.SetSurfaceOverrideMaterial(surface, material);
		}
	}

	private static Material? MakeTranslucent(Material? active)
	{
		switch (active is null ? new StandardMaterial3D() : active.Duplicate())
		{
			case BaseMaterial3D material:
				material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
				material.AlbedoColor = material.AlbedoColor with { A = material.AlbedoColor.A * PreviewOpacity };

				return material;

			case ShaderMaterial material when
				material.HasMeta(TranslucentShaderMeta) && material.GetMeta(TranslucentShaderMeta).Obj is Shader shader:
				material.Shader = shader;
				material.SetShaderParameter(OpacityParameter, PreviewOpacity);

				return material;

			default:
				return null;
		}
	}

	private void DestroyPreview()
	{
		_preview?.QueueFree();
		_preview = null;
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

		SetRotation(RotationSpace switch
		{
			RotationSpace.Global => increment * _rotation,
			RotationSpace.Local => _rotation * increment,
			_ => throw new UnreachableException()
		});
	}
}
