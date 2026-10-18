using System.Globalization;
using Godot;

// ReSharper disable UnusedMember.Global

namespace EnsembleRoot.Scripts.Assets;

/// <summary>
///     The rigid body of a group of welded instances. Each member's colliders become shapes of this body, while the
///     member itself rides along as a child, out of the physics space.
/// </summary>
public partial class Assembly : RigidBody3D
{
	private readonly List<AssetHandle> _members = [];

	private float? _bounce;
	private float _frictionMoment;
	private float _mass;
	private Vector3 _massMoment;

	/// <summary>The group's lowest instance ID, which every peer derives alike.</summary>
	public int Id { get; init; }

	public IReadOnlyList<AssetHandle> Members => _members;

	public bool IsKilled { get; private set; }

	/// <summary>
	///     Whether this peer simulates the body; otherwise it's a proxy, frozen as kinematic and moved by received
	///     state, so characters still collide with and stand on it. A massless body stays frozen either way.
	/// </summary>
	public bool IsSimulated
	{
		get;
		set
		{
			field = value;
			FreezeMode = value ? FreezeModeEnum.Static : FreezeModeEnum.Kinematic;
			Freeze = !value || _mass <= 0;
		}
	}

	// Best done before entering the tree, so the body enters the physics space with all of its shapes at once
	public void AddMember(AssetHandle member, AssetMaterial? material)
	{
		var transform = member.Transform;

		member.Assembly = this;
		member.Freeze = true;
		AddChild(member);

		foreach (var (shape, colliderTransform) in GAssetManager.GetColliders(member.AssetId))
		{
			var owner = CreateShapeOwner(member);

			ShapeOwnerAddShape(owner, shape);
			ShapeOwnerSetTransform(owner, transform * colliderTransform);
		}

		var (mass, centerOfMass) = GAssetManager.GetMass(member.AssetId, material);

		_mass += mass;
		_massMoment += transform * centerOfMass * mass;
		_frictionMoment += (material?.Friction ?? AssetMaterial.DefaultFriction) * mass;
		_bounce = MathF.Min(_bounce ?? float.PositiveInfinity, material?.Bounce ?? 0);
		_members.Add(member);
	}

	/// <summary>
	///     Takes the members' own bodies out of the physics space, then gives this body their mass, mass-weighted
	///     friction, and lowest bounce. It stays frozen until <see cref="IsSimulated" /> is set.
	/// </summary>
	/// <remarks>The assembly must be in the tree, so the members' bodies have entered the space they leave.</remarks>
	public void Assemble()
	{
		foreach (var member in _members)
			PhysicsServer3D.BodySetSpace(member.GetRid(), default);

		if (_mass <= 0)
			return;

		var friction = _frictionMoment / _mass;
		var bounce = _bounce ?? 0;

		Mass = _mass;
		CenterOfMassMode = CenterOfMassModeEnum.Custom;
		CenterOfMass = _massMoment / _mass;

		// Godot's defaults need no material of their own
		if (friction is not AssetMaterial.DefaultFriction || bounce is not 0)
			PhysicsMaterialOverride = new PhysicsMaterial { Friction = friction, Bounce = bounce };
	}

	/// <summary>
	///     Switches the assembly off (out of the physics space, hidden), as only despawning frees a creation.
	/// </summary>
	public void Kill()
	{
		IsKilled = true;
		ProcessMode = ProcessModeEnum.Disabled;
		Visible = false;
	}

	public override string ToString() =>
		string.Create(
			CultureInfo.InvariantCulture,
			$"Assembly(id={Id}, members={_members.Count}, mass={Mass:F1}, sleeping={Sleeping}, killed={IsKilled})");
}
