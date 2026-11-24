using System.Runtime.InteropServices;
using Godot;

namespace EnsembleRoot.Scripts.Creations;

/// <summary>An assembly's motion in world space, whose transform's origin is its center of mass.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct BodyState(Transform3D Transform, Vector3 LinearVelocity, Vector3 AngularVelocity)
{
	/// <summary>Gets the velocity of a point carried by the body, in world space.</summary>
	public Vector3 GetVelocityAt(Vector3 position) =>
		LinearVelocity + AngularVelocity.Cross(position - Transform.Origin);
}
