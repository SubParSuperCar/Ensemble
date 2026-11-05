using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Creations;
using Godot;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeProtected.Global

namespace EnsembleRoot.BuildAssets.Advanced.Hinges;

/// <summary>
///     A head turning against its base, driven by a pair of keys at a speed and torque from the instance's properties.
///     If it toggles, a key press starts it instead of holding the key, until the other key reverses it or the same key
///     stops it.
/// </summary>
public abstract partial class DrivenHinge : AssetHandle, ISimulationTicked, IReplicatedState
{
	private const string ForwardBinding = "forwardKey";
	private const string ReverseBinding = "reverseKey";
	private const string SpeedKey = "speed";
	private const string TorqueKey = "torque";
	private const string TogglesKey = "toggles";

	// Revolutions per minute and newton-meters
	private const float MaxSpeed = 1000f;
	private const float MaxTorque = 20_000f;

	private int _drivenDirection;
	private Key _forwardKey;

	// Nothing written yet, as a joint's own defaults aren't what the first write may want
	private float _impulse = float.NaN;

	private Key _reverseKey;
	private bool _toggles;
	private float _velocity = float.NaN;

	/// <summary>Links the base, as node A, to the head, as node B, about its Z axis.</summary>
	[Export]
	public HingeJoint3D Hinge { get; set; } = null!;

	[Export] public AssetPiece Base { get; set; } = null!;
	[Export] public AssetPiece Head { get; set; } = null!;

	/// <summary>Which way the keys drive it: 1 forward, -1 in reverse, or 0.</summary>
	protected int Direction { get; private set; }

	/// <summary>Whether both keys are held against each other, which cancels them out.</summary>
	protected bool IsOpposed { get; private set; }

	/// <summary>Radians per second, negative to reverse.</summary>
	protected float Speed { get; private set; }

	/// <summary>Newton-meters.</summary>
	protected float Torque { get; private set; }

	// Only a toggle's latch outlasts the keys that set it
	public int State
	{
		get => _toggles ? Direction : 0;
		set
		{
			if (_toggles)
				Direction = Math.Sign(value);
		}
	}

	public void Tick(in SimulationTick tick)
	{
		var isForward = tick.Input.IsPressed(_forwardKey);
		var isReverse = tick.Input.IsPressed(_reverseKey);

		Direction = _toggles ? GetToggledDirection(tick) : GetSign(isForward, isReverse);
		IsOpposed = !_toggles && isForward && isReverse;

		Drive(Direction != _drivenDirection, tick.Delta);
		_drivenDirection = Direction;
	}

	protected override void OnInitialized()
	{
		if (!IsLive)
			return;

		_forwardKey = GetKeyProperty(ForwardBinding);
		_reverseKey = GetKeyProperty(ReverseBinding);
		_toggles = Instance.Properties.GetValue(TogglesKey).AsBool();

		Speed = GetNumberProperty(SpeedKey, -MaxSpeed, MaxSpeed) * MathF.Tau / TimeSpan.SecondsPerMinute;
		Torque = GetNumberProperty(TorqueKey, 0, MaxTorque);
	}

	protected abstract void Drive(bool isDirectionChanged, double delta);

	// A positive target velocity spins node B backward about the joint's Z axis. Writing to a joint wakes its bodies, so
	// only changes are written, and an idle creation can fall asleep
	// ReSharper disable once CompareOfFloatsByEqualityOperator
	protected void SetVelocity(float velocity)
	{
		if (velocity == _velocity)
			return;

		_velocity = velocity;
		Hinge.SetParam(HingeJoint3D.Param.MotorTargetVelocity, -velocity);
	}

	// ReSharper disable once CompareOfFloatsByEqualityOperator
	protected void SetTorque(float torque, double delta)
	{
		var impulse = torque * (float)delta;

		if (impulse == _impulse)
			return;

		_impulse = impulse;
		Hinge.SetParam(HingeJoint3D.Param.MotorMaxImpulse, impulse);
	}

	private static int GetSign(bool isForward, bool isReverse) => (isForward ? 1 : 0) - (isReverse ? 1 : 0);

	// Only new presses count, and pressing both at once changes nothing, as holding both does otherwise
	private int GetToggledDirection(in SimulationTick tick)
	{
		var pressed = GetSign(tick.IsJustPressed(_forwardKey), tick.IsJustPressed(_reverseKey));

		return pressed is 0 ? Direction : pressed == Direction ? 0 : pressed;
	}
}
