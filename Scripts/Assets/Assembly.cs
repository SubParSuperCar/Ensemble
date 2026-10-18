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

	private float _mass;
	private Vector3 _massMoment;

	/// <summary>The group's lowest instance ID, which every peer derives alike.</summary>
	public int Id { get; init; }

	public IReadOnlyList<AssetHandle> Members => _members;

	public bool IsKilled { get; private set; }

	// Best done before entering the tree, so the body enters the physics space with all of its shapes at once
	public void AddMember(AssetHandle member)
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

		var (mass, centerOfMass) = GAssetManager.GetMass(member.AssetId);

		_mass += mass;
		_massMoment += transform * centerOfMass * mass;
		_members.Add(member);
	}

	/// <summary>
	///     Takes the members' own bodies out of the physics space, then releases this body into the simulation with
	///     their mass, unless it has none.
	/// </summary>
	/// <remarks>The assembly must be in the tree, so the members' bodies have entered the space they leave.</remarks>
	public void Release()
	{
		foreach (var member in _members)
			PhysicsServer3D.BodySetSpace(member.GetRid(), default);

		if (_mass <= 0)
			return;

		Mass = _mass;
		CenterOfMassMode = CenterOfMassModeEnum.Custom;
		CenterOfMass = _massMoment / _mass;
		Freeze = false;
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
