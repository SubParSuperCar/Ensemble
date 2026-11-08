using EnsembleRoot.Scripts.Creations;
using Godot;

namespace EnsembleRoot.BuildAssets.Advanced.Hinges;

/// <summary>
///     Turns its head to an angle: while driven, toward the limit in that direction, at its speed; otherwise back to
///     center, unless it holds where it is. Holding both keys holds it where it is. A limit of 180 degrees, either way,
///     lets it turn a full circle.
/// </summary>
public partial class Servo : DrivenHinge
{
	private const string MaxAngleKey = "maxAngle";
	private const string ReturnsToCenterKey = "returnsToCenter";

	// Degrees, either way from center
	private const float MaxAngleLimit = 180f;

	// Past the servo's reach, so the joint's limit only catches what overpowers the motor, without fighting it
	private const float LimitMargin = 1 / 16f;

	// Per second, of the angle left to go. Any higher and the arm chatters, as the motor answers a physics tick late
	private const float Gain = 8f;

	// Radians close enough to stop writing to the joint, so that a settled creation can fall asleep
	private const float SettledAngle = 1 / 512f;

	private Vector3 _axis;
	private Quaternion _builtRotation;
	private bool _isHolding;
	private float _maxAngle;
	private bool _returnsToCenter;
	private float _target;

	protected override void OnInitialized()
	{
		base.OnInitialized();

		if (!IsLive)
			return;

		_maxAngle = float.DegreesToRadians(GetNumberProperty(MaxAngleKey, 0, MaxAngleLimit));
		_returnsToCenter = Instance.Properties.GetValue(ReturnsToCenterKey).AsBool();

		// Nothing has moved yet, so this is the angle as built
		var baseBasis = Base.Assembly!.GlobalBasis;
		_builtRotation = (baseBasis.Inverse() * Head.Assembly!.GlobalBasis).GetRotationQuaternion();
		_axis = baseBasis.Inverse() * Hinge.GlobalBasis.Z;

		Hinge.SetFlag(HingeJoint3D.Flag.EnableMotor, true);

		if (_maxAngle >= float.Pi)
			return;

		Hinge.SetFlag(HingeJoint3D.Flag.UseLimit, true);
		Hinge.SetParam(HingeJoint3D.Param.LimitUpper, _maxAngle + LimitMargin);
		Hinge.SetParam(HingeJoint3D.Param.LimitLower, -_maxAngle - LimitMargin);
	}

	protected override void Drive(bool isDirectionChanged, in SimulationTick tick)
	{
		var angle = GetAngle();
		var isHolding = IsOpposed || (Direction is 0 && !_returnsToCenter);

		// What it held before another peer simulated it may have moved since
		if (isHolding && (!_isHolding || tick.IsResuming))
			_target = angle;

		_isHolding = isHolding;

		if (!isHolding)
			_target = Direction * MathF.Sign(Speed) * _maxAngle;

		var error = Mathf.Wrap(_target - angle, -float.Pi, float.Pi);
		var speed = MathF.Abs(Speed);

		SetVelocity(MathF.Abs(error) < SettledAngle ? 0 : Math.Clamp(error * Gain, -speed, speed));
		SetTorque(Torque, tick.Delta);
	}

	// How far the head has turned about the hinge since it was built
	private float GetAngle()
	{
		var relative = (Base.Assembly!.GlobalBasis.Inverse() * Head.Assembly!.GlobalBasis).GetRotationQuaternion();
		var turn = relative * _builtRotation.Inverse();

		return Mathf.Wrap(2 * MathF.Atan2(new Vector3(turn.X, turn.Y, turn.Z).Dot(_axis), turn.W), -float.Pi, float.Pi);
	}
}
