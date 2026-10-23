using System.Globalization;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Assets.Materials;
using Godot;

// ReSharper disable UnusedMember.Global

namespace EnsembleRoot.Scripts.Creations;

/// <summary>
///     The rigid body of a group of welded pieces (an instance's own colliders, or one of its <see cref="AssetPiece" />
///     children). Each piece's colliders become shapes of this body, while the piece's own body rides along as a
///     child, out of the physics space.
/// </summary>
public partial class Assembly : RigidBody3D
{
	private readonly List<CollisionObject3D> _pieces = [];

	private float? _bounce;
	private Aabb? _bounds;
	private float _frictionMoment;
	private float _mass;
	private Vector3 _massMoment;

	/// <summary>The group's index in weld order, which every peer derives alike.</summary>
	public int Id { get; init; }

	/// <summary>The pieces' bodies: handles (for their own colliders) and <see cref="AssetPiece" /> nodes.</summary>
	public IReadOnlyList<CollisionObject3D> Pieces => _pieces;

	/// <summary>The bounds of the pieces' colliders, in the body's space, once built.</summary>
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

	/// <summary>
	///     Adopts a piece's body as a child, registers the piece's colliders as shapes, and weighs it into the body.
	///     The body is a handle for piece 0, placed at <paramref name="handleTransform" /> (in this body's space), or
	///     else an <see cref="AssetPiece" />, placed where it sits relative to that handle.
	/// </summary>
	/// <remarks>
	///     Best done before entering the tree, so the body enters the physics space with all of its shapes at once.
	/// </remarks>
	public void AddPiece(
		CollisionObject3D body, Transform3D handleTransform, int assetId, int piece, AssetMaterial? material)
	{
		var transform = piece is 0 ? handleTransform : handleTransform * body.Transform;

		// A piece leaves its asset's scene, whose root would stay its owner (owners only matter for saving scenes)
		body.GetParent()?.RemoveChild(body);
		body.Owner = null;
		body.Transform = transform;
		AddChild(body);

		foreach (var (shape, colliderTransform) in GAssetManager.GetColliders(assetId, piece))
		{
			var owner = CreateShapeOwner(body);

			ShapeOwnerAddShape(owner, shape);
			ShapeOwnerSetTransform(owner, handleTransform * colliderTransform);
		}

		foreach (var bounds in GAssetManager.GetColliderBounds(assetId, piece))
			_bounds = _bounds?.Merge(handleTransform * bounds) ?? handleTransform * bounds;

		var (mass, centerOfMass) = GAssetManager.GetMass(assetId, piece, material);

		_mass += mass;
		_massMoment += handleTransform * centerOfMass * mass;
		_frictionMoment += (material?.Friction ?? AssetMaterial.DefaultFriction) * mass;
		_bounce = MathF.Min(_bounce ?? float.PositiveInfinity, material?.Bounce ?? AssetMaterial.DefaultBounce);
		_pieces.Add(body);
	}

	/// <summary>
	///     Gives the body its pieces' mass, mass-weighted friction, and lowest bounce, and moves its origin onto the
	///     center of mass, so its position and streamed velocities describe the same point it rotates about. It stays
	///     frozen until <see cref="IsSimulated" /> is set.
	/// </summary>
	/// <remarks>Best done before entering the tree, like <see cref="AddPiece" />.</remarks>
	public void Build()
	{
		var center = _mass > 0 ? _massMoment / _mass : Vector3.Zero;

		if (_bounds is { } bounds)
			Bounds = bounds with { Position = bounds.Position - center };

		if (_mass <= 0)
			return;

		foreach (var body in _pieces)
			body.Transform = body.Transform.Translated(-center);

		foreach (var owner in GetShapeOwners())
			ShapeOwnerSetTransform((uint)owner, ShapeOwnerGetTransform((uint)owner).Translated(-center));

		Transform = Transform.TranslatedLocal(center);

		var friction = _frictionMoment / _mass;
		var bounce = _bounce ?? AssetMaterial.DefaultBounce;

		Mass = _mass;
		CenterOfMassMode = CenterOfMassModeEnum.Custom;
		CenterOfMass = Vector3.Zero;

		// Godot's defaults (friction 1, bounce 0), not the asset materials', need no material of their own
		if (friction is not 1f || bounce is not 0f)
			PhysicsMaterialOverride = new PhysicsMaterial { Friction = friction, Bounce = bounce };
	}

	/// <summary>Takes the pieces' own bodies out of the physics space, leaving only this body's shapes.</summary>
	/// <remarks>The assembly must be in the tree, so the pieces' bodies have entered the space they leave.</remarks>
	public void Assemble()
	{
		foreach (var body in _pieces)
			PhysicsServer3D.BodySetSpace(body.GetRid(), default);
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
			$"Assembly(id={Id}, pieces={_pieces.Count}, mass={Mass:F1}, sleeping={Sleeping}, killed={IsKilled})");
}
