using EnsembleRoot.Scripts.Assets.Impl;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Assets;

[GlobalClass]
public partial class AssetHandle : RigidBody3D
{
	// Water's, until materials have their own
	public const float DefaultDensity = 1000f;

	private bool _isBoundaryCalculated;

	[Export(PropertyHint.Range, "0,0,1,or_greater,hide_slider")]
	public int AssetId { get; set; }

	[Export] public StringName AssetName { get; set; } = null!;

	[Export] public Godot.Collections.Dictionary<StringName, Variant> Properties { get; set; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxInstanceCount { get; set; }

	[Export(PropertyHint.Range, "0,0,0.001,or_greater,hide_slider,suffix:kg/m\u00b3")]
	public float Density { get; set; } = DefaultDensity;

	// A zero size means unset; the flag keeps a calculated zero size (e.g., no collider) from recalculating
	[Export]
	public Aabb BoundaryAabb
	{
		get
		{
			if (_isBoundaryCalculated || field.Size != Vector3.Zero)
				return field;

			_isBoundaryCalculated = true;
			return field = CalculateBoundary();
		}
		set;
	}

	public int InstanceId { get; internal set; }

	/// <summary>The assembly carrying this handle while its plot is spawned, set for the handle's whole life.</summary>
	public Assembly? Assembly { get; internal set; }

	public bool IsLive => Assembly is not null;

	// Only direct children count, as a body only registers the shapes of its direct children
	public IEnumerable<CollisionShape3D> GetColliders() =>
		GetChildren()
			.OfType<CollisionShape3D>()
			.Where(static collider => collider is { Shape: not null, Disabled: false });

	public Aabb[] GetColliderBounds() =>
		[.. GetColliders().Select(static collider => collider.Transform * collider.Shape.GetDebugMesh().GetAabb())];

	/// <summary>Gets the mass, from <see cref="Density" /> and the colliders' volume, and the center of mass.</summary>
	public (float Mass, Vector3 CenterOfMass) CalculateMass()
	{
		var volume = 0f;
		var moment = Vector3.Zero;

		foreach (var collider in GetColliders())
		{
			var (shapeVolume, centroid) = Shapes.Measure(collider.Shape);
			var scaledVolume = shapeVolume * MathF.Abs(collider.Transform.Basis.Determinant());

			volume += scaledVolume;
			moment += collider.Transform * centroid * scaledVolume;
		}

		return volume > 0 ? (Density * volume, moment / volume) : default;
	}

	private Aabb CalculateBoundary() =>
		GetColliderBounds() is [var first, .. var rest]
			? rest.Aggregate(first, static (sum, aabb) => sum.Merge(aabb))
			: default;
}
