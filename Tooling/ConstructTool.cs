using System.Diagnostics;
using Godot;
using Root.Common.Input;
using Root.Scripts.Assets;

namespace Root.Tooling;

// TODO: Very early W.I.P.
public partial class ConstructTool : ToolBase
{
	private static readonly StringName TriggerAction = "tool_trigger";

	private static readonly StringName RotateXAction = "tool_ctor_rot_x";
	private static readonly StringName RotateYAction = "tool_ctor_rot_y";
	private static readonly StringName RotateZAction = "tool_ctor_rot_z";

	private bool _canPlace;
	private AssetHandle? _handle;
	private bool _isActive;
	private Vector3 _position;
	private Quaternion _rotation;

	protected override StringName ToggleAction => "tool_construct_toggle";

	public RotationSpace RotationSpace { get; set; } = RotationSpace.Global;

	public float? SnappingIncrementLinear { get; set; } = 1;
	public float SnappingIncrementAngularRadians { get; set; } = MathF.PI / 2;

	public int AssetId { get; private set; }
	public event Action<int>? AssetIdChanged;

	protected override void OnEnable() { }
	protected override void OnDisable() { }

	public override void _Process(double delta) { }

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_isActive || InputSink.IsSunk)
			return;

		if (@event.IsActionPressed(RotateXAction))
			Rotate(Vector3.Right);
		else if (@event.IsActionPressed(RotateYAction))
			Rotate(Vector3.Up);
		else if (@event.IsActionPressed(RotateZAction))
			Rotate(Vector3.Back);
		else if (@event.IsActionPressed(TriggerAction) && _canPlace)
			LocalPlot?.Instances.Add(AssetId, _position, _rotation);
	}

	public void SetAsset(int id)
	{
		if (AssetId == id)
			return;

		AssetId = id;
		AssetIdChanged?.Invoke(id);
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

	/*private void ActivateAsset() { }
	private void DeactivateAsset() { }*/

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
