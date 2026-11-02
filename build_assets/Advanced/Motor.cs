using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Creations;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.BuildAssets.Advanced;

/// <summary>
///     Spins its head against its base while its keys are held, at a speed and torque from the instance's properties;
///     a negative speed reverses it. With neither key held, or both, it idles: free to coast, unless it brakes. If it
///     toggles, a key press starts it instead, until the other key reverses it or the same key stops it. Its torque is
///     constant, or falls off in a straight line to nothing at full speed, like an electric motor's.
/// </summary>
public partial class Motor : AssetHandle, ISimulationTicked
{
	private const string ForwardBinding = "forwardKey";
	private const string ReverseBinding = "reverseKey";
	private const string SpeedKey = "speed";
	private const string TorqueKey = "torque";
	private const string BrakesWhenIdleKey = "brakesWhenIdle";
	private const string TorqueFalloffKey = "torqueFalloff";
	private const string TogglesKey = "toggles";

	// Revolutions per minute and newton-meters
	private const float MaxSpeed = 1000f;
	private const float MaxTorque = 20_000f;

	private const float RadiansPerRevolution = MathF.Tau;
	private const float SecondsPerMinute = 60f;

	private bool _brakesWhenIdle;
	private int _direction;
	private Key _forwardKey;
	private bool _hasTorqueFalloff;
	private float _impulse;
	private bool _isForwardHeld;
	private bool _isReverseHeld;
	private Key _reverseKey;
	private float _speed;
	private bool _toggles;
	private float _torque;

	/// <summary>Links the base, as node A, to the head, as node B, about its Z axis.</summary>
	[Export]
	public HingeJoint3D Hinge { get; set; } = null!;

	[Export] public AssetPiece Base { get; set; } = null!;
	[Export] public AssetPiece Head { get; set; } = null!;

	public void Tick(in SimulationTick tick)
	{
		var isForward = tick.Input.IsPressed(_forwardKey);
		var isReverse = tick.Input.IsPressed(_reverseKey);
		var direction = _toggles ? GetToggledDirection(isForward, isReverse) : GetSign(isForward, isReverse);

		_isForwardHeld = isForward;
		_isReverseHeld = isReverse;

		// Writing to a joint wakes its bodies, so only changes are written, and an idle creation can fall asleep
		if (direction != _direction)
		{
			_direction = direction;
			ApplyDirection();
		}

		SetImpulse(GetTorque() * (float)tick.Delta);
	}

	protected override void OnInitialized()
	{
		if (!IsLive)
			return;

		_forwardKey = GetKeyProperty(ForwardBinding);
		_reverseKey = GetKeyProperty(ReverseBinding);

		_speed = GetNumberProperty(SpeedKey, -MaxSpeed, MaxSpeed) * RadiansPerRevolution / SecondsPerMinute;
		_torque = GetNumberProperty(TorqueKey, 0, MaxTorque);

		_brakesWhenIdle = Instance.Properties.GetValue(BrakesWhenIdleKey).AsBool();
		_hasTorqueFalloff = Instance.Properties.GetValue(TorqueFalloffKey).AsBool() && _speed is not 0;
		_toggles = Instance.Properties.GetValue(TogglesKey).AsBool();

		ApplyDirection();
	}

	private static int GetSign(bool isForward, bool isReverse) => (isForward ? 1 : 0) - (isReverse ? 1 : 0);

	// Only new presses count, and pressing both at once changes nothing, as holding both does otherwise
	private int GetToggledDirection(bool isForward, bool isReverse)
	{
		var pressed = GetSign(isForward && !_isForwardHeld, isReverse && !_isReverseHeld);

		return pressed is 0 ? _direction : pressed == _direction ? 0 : pressed;
	}

	// A positive target velocity spins node B backward about the joint's Z axis
	private void ApplyDirection()
	{
		Hinge.SetFlag(HingeJoint3D.Flag.EnableMotor, _direction is not 0 || _brakesWhenIdle);
		Hinge.SetParam(HingeJoint3D.Param.MotorTargetVelocity, -_direction * _speed);
	}

	private float GetTorque()
	{
		if (!_hasTorqueFalloff || _direction is 0)
			return _torque;

		var spin = (Head.Assembly!.AngularVelocity - Base.Assembly!.AngularVelocity).Dot(Hinge.GlobalBasis.Z);
		return _torque * Math.Clamp(1 - spin / (_direction * _speed), 0, 1);
	}

	// ReSharper disable once CompareOfFloatsByEqualityOperator
	private void SetImpulse(float impulse)
	{
		if (impulse == _impulse)
			return;

		_impulse = impulse;
		Hinge.SetParam(HingeJoint3D.Param.MotorMaxImpulse, impulse);
	}
}
