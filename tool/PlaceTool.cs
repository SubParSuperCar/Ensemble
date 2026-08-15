using System.Diagnostics;
using Godot;
using Root.Common.Globals;
using Root.Common.Input;
using Root.Core.Gd.Asset;
using Root.Scripts.Asset;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable EventNeverSubscribedTo.Global

namespace Root.Tool;

// TODO
public partial class PlaceTool : ToolBase
{
	private readonly StringName _rotateXAction = "tool_place_rotate_x";
	private readonly StringName _rotateYAction = "tool_place_rotate_y";
	private readonly StringName _rotateZAction = "tool_place_rotate_z";
	private readonly StringName _triggerAction = "tool_place_trigger";

	private GdAsset? _asset;
	private bool _canPlace;
	private AssetHandle? _handle;
	private bool _isActive;
	private Vector3 _position;
	private Quaternion _rotation;
	private Vector3 _size;

	protected override StringName ToggleAction => "tool_place_toggle";

	public RotationSpace RotationSpace { get; set; } = RotationSpace.Global;

	// ReSharper disable once UnusedMember.Global
	public float? SnappingIncrementLinear { get; set; } = 1;
	public float SnappingIncrementAngularRadians { get; set; } = MathF.PI / 2;

	public int AssetId { get; private set; }
	public event Action<int>? AssetIdChanged;

	protected override void OnEnable() { }
	protected override void OnDisable() { }

	public override void _Process(double delta) { }

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_isActive || InputExtensions.IsSunk)
			return;

		if (@event.IsActionPressed(_rotateXAction))
			Rotate(Vector3.Right);
		else if (@event.IsActionPressed(_rotateYAction))
			Rotate(Vector3.Up);
		else if (@event.IsActionPressed(_rotateZAction))
			Rotate(Vector3.Back);
		else if (@event.IsActionPressed(_triggerAction) && _canPlace)
			GContext.Plot?.Instances.Add(AssetId, _position, _rotation);
	}

	private void Activate()
	{
		if (_isActive)
			return;

		_isActive = true;
	}

	private void Deactivate()
	{
		if (!_isActive)
			return;

		_isActive = false;
	}

	private void Rotate(Vector3 axis)
	{
		var increment = new Quaternion(axis, SnappingIncrementAngularRadians);

		_rotation = RotationSpace switch
		{
			RotationSpace.Global => increment * _rotation,
			RotationSpace.Local => _rotation * increment,
			_ => throw new UnreachableException()
		};
	}
}

public enum RotationSpace
{
	Global,
	Local
}
