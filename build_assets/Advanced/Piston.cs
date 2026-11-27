using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Creations;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.BuildAssets.Advanced;

/// <summary>
///     A plate pushed out of its base along the ram, like a hydraulic ram: while driven, out to its maximum length or
///     back in, at its speed, with no more than its force, so a heavy enough load still pushes it in. Otherwise it
///     retracts, unless it holds where it is; holding both keys holds it too. Its length is how far the plate is out
///     from fully in, in grid units, and its keys toggle like a motor's if it toggles. While live, the shaft between
///     base and plate stretches with them.
/// </summary>
public partial class Piston : AssetHandle, ISimulationTicked, IReplicatedState
{
	private const string ExtendBinding = "extendKey";
	private const string RetractBinding = "retractKey";
	private const string TogglesKey = "toggles";
	private const string AutoRetractKey = "autoRetract";
	private const string MaxLengthKey = "maxLength";
	private const string SpeedKey = "speed";
	private const string ForceKey = "force";

	// The farthest its plate may go out; its speed, read in meters per second; and newtons
	private const float MaxReach = 8 * GridToWorldScale;
	private const float MaxSpeed = 16f;
	private const float MaxForce = 20_000f;

	// Per second, of the length left to go, as the servo's; any higher and the plate chatters
	private const float Gain = 8f;

	// Meters close enough to stop writing to the joint, so that a settled creation can fall asleep
	private const float SettledLength = 1 / 512f;

	// Past its reach, so the joint's limit only catches what overpowers the motor, without fighting it
	private const float LimitMargin = 1 / 32f;

	private bool _autoRetracts;
	private Vector3 _axis;
	private Vector3 _builtOffset;
	private bool _isHolding;
	private DrivingKeys _keys = null!;
	private float _reach;
	private Vector3 _shaftBottom;
	private float _shaftLength;
	private float _speed;
	private float _target;

	// Nothing written yet, as the joint's own default isn't what the first write may want
	private float _velocity = float.NaN;

	/// <summary>Links the base, as node A, to the plate, as node B, along its Y axis.</summary>
	[Export]
	public Generic6DofJoint3D Ram { get; set; } = null!;

	[Export] public AssetPiece Base { get; set; } = null!;
	[Export] public AssetPiece Plate { get; set; } = null!;

	/// <summary>The shaft, standing in the base, a mesh one meter tall scaled to its length.</summary>
	[Export]
	public MeshInstance3D Shaft { get; set; } = null!;

	/// <summary>Where the shaft meets the plate.</summary>
	[Export]
	public Node3D ShaftEnd { get; set; } = null!;

	public int State
	{
		get => _keys.State;
		set => _keys.State = value;
	}

	public void Tick(in SimulationTick tick)
	{
		_keys.Update(tick);

		var extension = GetExtension();
		var isHolding = _keys.IsOpposed || (_keys.Direction is 0 && !_autoRetracts);

		// What it held before another peer simulated it may have moved since
		if (isHolding && (!_isHolding || tick.IsResuming))
			_target = extension;

		_isHolding = isHolding;

		if (!isHolding)
			_target = _keys.Direction > 0 ? _reach : 0;

		var error = _target - extension;
		SetVelocity(MathF.Abs(error) < SettledLength ? 0 : Math.Clamp(error * Gain, -_speed, _speed));
	}

	// Built and previewed pistons show the shaft as authored
	public override void _Ready()
	{
		SetPhysicsProcess(IsLive);

		_shaftLength = Shaft.Scale.Y;
		_shaftBottom = Shaft.Position with { Y = Shaft.Position.Y - _shaftLength / 2 };
	}

	// On physics ticks, in the base's space, so the shaft interpolates along with the pieces it spans
	public override void _PhysicsProcess(double delta)
	{
		var end = Shaft.GetParentNode3D().GlobalTransform.AffineInverse() * ShaftEnd.GlobalPosition;
		var length = end.Y - _shaftBottom.Y;

		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if (length == _shaftLength)
			return;

		_shaftLength = length;
		Shaft.Transform = new Transform3D(
			Basis.FromScale(new Vector3(1, length, 1)), _shaftBottom with { Y = _shaftBottom.Y + length / 2 });
	}

	// The joint measures the plate's travel from where it was built, all the way in
	protected override void OnInitialized()
	{
		if (!IsLive)
			return;

		_keys = new DrivingKeys(
			GetKeyProperty(ExtendBinding), GetKeyProperty(RetractBinding),
			Instance.Properties.GetValue(TogglesKey).AsBool());

		_autoRetracts = Instance.Properties.GetValue(AutoRetractKey).AsBool();
		_speed = GetLengthProperty(SpeedKey, 0, MaxSpeed);

		_reach = GetLengthProperty(MaxLengthKey, 0, MaxReach);

		// Nothing has moved yet, so this is the plate as built
		var baseInverse = Base.Assembly!.GlobalTransform.AffineInverse();
		_builtOffset = baseInverse * Plate.Assembly!.GlobalPosition;
		_axis = baseInverse.Basis * Ram.GlobalBasis.Y;

		Ram.SetFlagY(Generic6DofJoint3D.Flag.EnableLinearLimit, true);
		Ram.SetParamY(Generic6DofJoint3D.Param.LinearLowerLimit, 0);
		Ram.SetParamY(Generic6DofJoint3D.Param.LinearUpperLimit, _reach + LimitMargin);

		Ram.SetFlagY(Generic6DofJoint3D.Flag.EnableLinearMotor, true);
		Ram.SetParamY(Generic6DofJoint3D.Param.LinearMotorForceLimit, GetNumberProperty(ForceKey, 0, MaxForce));
	}

	// How far the plate has moved out along the ram since it was built
	private float GetExtension()
	{
		var plate = Base.Assembly!.State.Transform.AffineInverse() * Plate.Assembly!.State.Transform.Origin;
		return (plate - _builtOffset).Dot(_axis);
	}

	// A positive target velocity moves node B out along the joint's Y axis. Writing to a joint wakes its bodies, so
	// only changes are written, and an idle creation can fall asleep
	// ReSharper disable once CompareOfFloatsByEqualityOperator
	private void SetVelocity(float velocity)
	{
		if (velocity == _velocity)
			return;

		_velocity = velocity;
		Ram.SetParamY(Generic6DofJoint3D.Param.LinearMotorTargetVelocity, velocity);
	}
}
