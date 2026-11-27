using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Creations;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.BuildAssets.Advanced.Thrusters;

/// <summary>
///     Pushes itself away from whatever its ray hits within its range, the harder the nearer, like a spring that only
///     pushes, damped so it settles instead of bobbing. Its power, which starts at half, scales both its range and its
///     force, and its keys raise or lower it while held, from none to full in its wind-up time. Its range is in grid
///     units, and its force in newtons. A part of its own creation it pushes off is pushed back unless it doesn't
///     react, and it can see past other creations.
/// </summary>
public partial class HoverThruster : AssetHandle, ISimulationTicked, IReplicatedState
{
	private const string PowerUpBinding = "powerUpKey";
	private const string PowerDownBinding = "powerDownKey";
	private const string MaxForceKey = "maxForce";
	private const string MaxLengthKey = "maxLength";
	private const string WindUpTimeKey = "windUpTime";
	private const string ReactionForceKey = "reactionForce";
	private const string IgnoresOtherCreationsKey = "ignoresOtherCreations";

	// Newtons and meters, at full power
	private const float MaxForceLimit = 20_000f;
	private const float MaxLengthLimit = 16 * GridToWorldScale;
	private const float MaxWindUpTime = 60f;

	private const float InitialPower = 1 / 2f;

	// Bodies of other creations a cast may see past, beyond which it gives up on the tick
	private const int MaxSkippedBodies = 4;

	// The damping per closing speed, as the spring's stiffness times this many seconds: enough to settle a light load
	private const float DampingTime = 1 / 4f;

	// The power's steps as an asset state
	private const int PowerSteps = 1 << 12;

	private const uint SensedLayers =
		PhysicsLayers.World | PhysicsLayers.StaticInstances | PhysicsLayers.DynamicInstances;

	private readonly List<Assembly> _skipped = [];

	private bool _ignoresOthers;
	private bool _isReacting;
	private float _maxForce;
	private float _maxLength;
	private float _power = InitialPower;
	private Key _powerDownKey;

	// Of full power per second, while a key is held
	private float _powerRate;

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

		// The rate is infinite for an instant wind-up, which an idle input of 0 would turn into NaN
		if (input is not 0)
			_power = Math.Clamp(_power + input * _powerRate * (float)tick.Delta, 0, 1);

		var range = _power * _maxLength;

		if (range <= 0)
			return;

		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if (range != _range)
		{
			_range = range;
			Ray.TargetPosition = Vector3.Down * range;
		}

		if (Cast() is not { } collider)
			return;

		var state = Assembly!.State;
		var origin = state.Transform * _rayOnAssembly.Origin;
		var direction = -(state.Transform.Basis * _rayOnAssembly.Basis.Y).Normalized();
		var distance = (Ray.GetCollisionPoint() - origin).Length();

		var strength = _power * _maxForce;
		var closing = state.GetVelocityAt(origin).Dot(direction);
		var push = strength * (1 - distance / range) + strength / range * DampingTime * closing;

		if (push <= 0)
			return;

		var force = direction * MathF.Min(push, strength);
		Assembly.AddForce(-force, origin);

		if (_isReacting && collider is Assembly other && other.Creation == Assembly.Creation)
			other.AddForce(force, Ray.GetCollisionPoint());
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

		var windUpTime = GetNumberProperty(WindUpTimeKey, 0, MaxWindUpTime);
		_powerRate = windUpTime > 0 ? 1 / windUpTime : float.PositiveInfinity;

		_isReacting = Instance.Properties.GetValue(ReactionForceKey).AsBool();
		_ignoresOthers = Instance.Properties.GetValue(IgnoresOtherCreationsKey).AsBool();

		// Other creations' built instances are left out whole; their live bodies are seen past one by one
		Ray.CollisionMask = _ignoresOthers ? PhysicsLayers.World | PhysicsLayers.DynamicInstances : SensedLayers;
		Ray.AddException(Assembly);
		_rayOnAssembly = Assembly!.GlobalTransform.AffineInverse() * Ray.GlobalTransform;
	}

	// What the ray hits, seeing past other creations' bodies if it ignores them, or null
	private GodotObject? Cast()
	{
		Ray.ForceRaycastUpdate();
		var collider = Ray.IsColliding() ? Ray.GetCollider() : null;

		while (_ignoresOthers && IsOther(collider) && _skipped.Count < MaxSkippedBodies)
		{
			var other = (Assembly)collider!;
			Ray.AddException(other);
			_skipped.Add(other);

			Ray.ForceRaycastUpdate();
			collider = Ray.IsColliding() ? Ray.GetCollider() : null;
		}

		foreach (var other in _skipped)
			Ray.RemoveException(other);

		_skipped.Clear();
		return _ignoresOthers && IsOther(collider) ? null : collider;
	}

	private bool IsOther(GodotObject? collider) => collider is Assembly other && other.Creation != Assembly!.Creation;
}
