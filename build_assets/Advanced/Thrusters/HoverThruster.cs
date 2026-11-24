using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Creations;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.BuildAssets.Advanced.Thrusters;

/// <summary>
///     Pushes itself away from whatever its ray hits within its range, the harder the nearer, like a spring that only
///     pushes, damped so it settles instead of bobbing. Its power, which starts at half, scales both its range and its
///     force, and its keys raise or lower it while held. Its range is in grid units, and its force in newtons.
/// </summary>
public partial class HoverThruster : AssetHandle, ISimulationTicked, IReplicatedState
{
	private const string PowerUpBinding = "powerUpKey";
	private const string PowerDownBinding = "powerDownKey";
	private const string MaxForceKey = "maxForce";
	private const string MaxLengthKey = "maxLength";

	// Newtons and meters, at full power
	private const float MaxForceLimit = 20_000f;
	private const float MaxLengthLimit = 16 * GridToWorldScale;

	private const float InitialPower = 1 / 2f;

	// Of full power per second, while a key is held
	private const float PowerRate = 1 / 2f;

	// The damping per closing speed, as the spring's stiffness times this many seconds: enough to settle a light load
	private const float DampingTime = 1 / 4f;

	// The power's steps as an asset state
	private const int PowerSteps = 1 << 12;

	private const uint SensedLayers =
		PhysicsLayers.World | PhysicsLayers.StaticInstances | PhysicsLayers.DynamicInstances;

	private float _maxForce;
	private float _maxLength;
	private float _power = InitialPower;
	private Key _powerDownKey;
	private Key _powerUpKey;
	private float _range = float.NaN;

	// The ray's place on its assembly, which moves it every tick
	private Transform3D _rayOnAssembly;

	/// <summary>Casts from the thruster's face, outward along its own -Y axis.</summary>
	[Export]
	public RayCast3D Ray { get; set; } = null!;

	public int State
	{
		get => (int)MathF.Round(_power * PowerSteps, MidpointRounding.AwayFromZero);
		set => _power = Math.Clamp(value / (float)PowerSteps, 0, 1);
	}

	public void Tick(in SimulationTick tick)
	{
		var input = (tick.Input.IsPressed(_powerUpKey) ? 1 : 0) - (tick.Input.IsPressed(_powerDownKey) ? 1 : 0);
		_power = Math.Clamp(_power + input * PowerRate * (float)tick.Delta, 0, 1);

		var range = _power * _maxLength;

		if (range <= 0)
			return;

		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if (range != _range)
		{
			_range = range;
			Ray.TargetPosition = Vector3.Down * range;
		}

		Ray.ForceRaycastUpdate();

		if (!Ray.IsColliding())
			return;

		var state = Assembly!.State;
		var origin = state.Transform * _rayOnAssembly.Origin;
		var direction = -(state.Transform.Basis * _rayOnAssembly.Basis.Y).Normalized();
		var distance = (Ray.GetCollisionPoint() - origin).Length();

		var strength = _power * _maxForce;
		var closing = state.GetVelocityAt(origin).Dot(direction);
		var push = strength * (1 - distance / range) + strength / range * DampingTime * closing;

		if (push > 0)
			Assembly.AddForce(-direction * MathF.Min(push, strength), origin);
	}

	// Rays cast on demand, and only on the simulator
	public override void _Ready() => Ray.Enabled = false;

	protected override void OnInitialized()
	{
		if (!IsLive)
			return;

		_powerUpKey = GetKeyProperty(PowerUpBinding);
		_powerDownKey = GetKeyProperty(PowerDownBinding);
		_maxForce = GetNumberProperty(MaxForceKey, 0, MaxForceLimit);
		_maxLength = GetLengthProperty(MaxLengthKey, 0, MaxLengthLimit);

		Ray.CollisionMask = SensedLayers;
		Ray.AddException(Assembly);
		_rayOnAssembly = Assembly!.GlobalTransform.AffineInverse() * Ray.GlobalTransform;
	}
}
