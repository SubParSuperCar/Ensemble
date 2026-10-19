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
	private Aabb? _bounds;
	private float _frictionMoment;
	private float _mass;
	private Vector3 _massMoment;

	/// <summary>The group's lowest instance ID, which every peer derives alike.</summary>
	public int Id { get; init; }

	public IReadOnlyList<AssetHandle> Members => _members;

	/// <summary>The bounds of the members' colliders, in the body's space, once built.</summary>
	public Aabb Bounds { get; private set; }

	public bool IsKilled { get; private set; }

	/// <summary>
	///     Whether this peer simulates the body; otherwise it's a proxy, frozen as kinematic and moved by received
	///     state, so characters still collide with and stand on it. A massless body stays frozen either way.
	/// </summary>
	public bool IsSimulated
	{
		get;
		internal set
		{
			field = value;
			FreezeMode = value ? FreezeModeEnum.Static : FreezeModeEnum.Kinematic;
			Freeze = !value || _mass <= 0;

			if (!value)
				ProxyTransform = GlobalTransform;
		}
	}

	/// <summary>Adopts a handle as a child, registers its colliders as shapes, and weighs it into the body.</summary>
	/// <remarks>
	///     Best done before entering the tree, so the body enters the physics space with all of its shapes at once.
	/// </remarks>
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

		foreach (var bounds in GAssetManager.GetColliderBounds(member.AssetId))
			_bounds = _bounds?.Merge(transform * bounds) ?? transform * bounds;

		var (mass, centerOfMass) = GAssetManager.GetMass(member.AssetId, material);

		_mass += mass;
		_massMoment += transform * centerOfMass * mass;
		_frictionMoment += (material?.Friction ?? AssetMaterial.DefaultFriction) * mass;
		_bounce = MathF.Min(_bounce ?? float.PositiveInfinity, material?.Bounce ?? 0);
		_members.Add(member);
	}

	/// <summary>
	///     Gives the body its members' mass, mass-weighted friction, and lowest bounce, and moves its origin onto the
	///     center of mass, so its position and streamed velocities describe the same point it rotates about. It stays
	///     frozen until <see cref="IsSimulated" /> is set.
	/// </summary>
	/// <remarks>Best done before entering the tree, like <see cref="AddMember" />.</remarks>
	public void Build()
	{
		var center = _mass > 0 ? _massMoment / _mass : Vector3.Zero;

		if (_bounds is { } bounds)
			Bounds = bounds with { Position = bounds.Position - center };

		if (_mass <= 0)
			return;

		foreach (var member in _members)
			member.Transform = member.Transform.Translated(-center);

		foreach (var owner in GetShapeOwners())
			ShapeOwnerSetTransform((uint)owner, ShapeOwnerGetTransform((uint)owner).Translated(-center));

		Transform = Transform.TranslatedLocal(center);

		var friction = _frictionMoment / _mass;
		var bounce = _bounce ?? 0;

		Mass = _mass;
		CenterOfMassMode = CenterOfMassModeEnum.Custom;
		CenterOfMass = Vector3.Zero;

		// Godot's defaults need no material of their own
		if (friction is not AssetMaterial.DefaultFriction || bounce is not 0)
			PhysicsMaterialOverride = new PhysicsMaterial { Friction = friction, Bounce = bounce };
	}

	/// <summary>Takes the members' own bodies out of the physics space, leaving only this body's shapes.</summary>
	/// <remarks>The assembly must be in the tree, so the members' bodies have entered the space they leave.</remarks>
	public void Assemble()
	{
		foreach (var member in _members)
			PhysicsServer3D.BodySetSpace(member.GetRid(), default);
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
