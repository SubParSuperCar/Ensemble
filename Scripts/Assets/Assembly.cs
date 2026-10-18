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

	/// <remarks>The assembly must be in the tree, so the member's own body has entered the space it leaves.</remarks>
	public void AddMember(AssetHandle member)
	{
		member.Assembly = this;
		member.Freeze = true;

		AddChild(member);
		PhysicsServer3D.BodySetSpace(member.GetRid(), default);

		foreach (var collider in member.GetColliders())
		{
			var owner = CreateShapeOwner(collider);

			ShapeOwnerAddShape(owner, collider.Shape);
			ShapeOwnerSetTransform(owner, member.Transform * collider.Transform);
		}

		var (mass, centerOfMass) = GAssetManager.GetMass(member.AssetId);

		_mass += mass;
		_massMoment += member.Transform * centerOfMass * mass;
		_members.Add(member);
	}

	/// <summary>Applies the members' mass and releases the body into the simulation, unless it has no mass.</summary>
	public void Release()
	{
		if (_mass <= 0)
			return;

		Mass = _mass;
		CenterOfMassMode = CenterOfMassModeEnum.Custom;
		CenterOfMass = _massMoment / _mass;
		Freeze = false;
	}

	/// <summary>Switches the assembly off (out of the physics space, hidden), as only despawning frees a creation.</summary>
	public void Kill()
	{
		IsKilled = true;
		ProcessMode = ProcessModeEnum.Disabled;
		Visible = false;
	}
}
