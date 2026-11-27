using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Creations;
using Godot;

// ReSharper disable CompareOfFloatsByEqualityOperator
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

	// Nothing written yet, as a joint's own defaults aren't what the first write may want
	private float _impulse = float.NaN;

	private DrivingKeys _keys = null!;
	private float _velocity = float.NaN;

	/// <summary>Links the base, as node A, to the head, as node B, about its Z axis.</summary>
	[Export]
	public HingeJoint3D Hinge { get; set; } = null!;

	[Export] public AssetPiece Base { get; set; } = null!;
	[Export] public AssetPiece Head { get; set; } = null!;

	/// <inheritdoc cref="DrivingKeys.Direction" />
	protected int Direction => _keys.Direction;

	/// <inheritdoc cref="DrivingKeys.IsOpposed" />
	protected bool IsOpposed => _keys.IsOpposed;

	/// <summary>Radians per second, negative to reverse.</summary>
	protected float Speed { get; private set; }

	/// <summary>Newton-meters.</summary>
	protected float Torque { get; private set; }

	public int State
	{
		get => _keys.State;
		set => _keys.State = value;
	}

	public void Tick(in SimulationTick tick)
	{
		_keys.Update(tick);

		Drive(Direction != _drivenDirection, tick);
		_drivenDirection = Direction;
	}

	protected override void OnInitialized()
	{
		if (!IsLive)
			return;

		_keys = new DrivingKeys(
			GetKeyProperty(ForwardBinding), GetKeyProperty(ReverseBinding),
			Instance.Properties.GetValue(TogglesKey).AsBool());

		Speed = GetNumberProperty(SpeedKey, -MaxSpeed, MaxSpeed) * float.Tau / TimeSpan.SecondsPerMinute;
		Torque = GetNumberProperty(TorqueKey, 0, MaxTorque);
	}

	protected abstract void Drive(bool isDirectionChanged, in SimulationTick tick);

	// A positive target velocity spins node B backward about the joint's Z axis. Writing to a joint wakes its bodies,
	// so only changes are written, and an idle creation can fall asleep.
	protected void SetVelocity(float velocity)
	{
		if (velocity == _velocity)
			return;

		_velocity = velocity;
		Hinge.SetParam(HingeJoint3D.Param.MotorTargetVelocity, -velocity);
	}

	protected void SetTorque(float torque, double delta)
	{
		var impulse = torque * (float)delta;

		if (impulse == _impulse)
			return;

		_impulse = impulse;
		Hinge.SetParam(HingeJoint3D.Param.MotorMaxImpulse, impulse);
	}
}
