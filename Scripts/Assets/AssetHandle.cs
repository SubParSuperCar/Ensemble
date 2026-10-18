using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Scripts.Assets.Impl;
using EnsembleRoot.Scripts.Plots;
using Godot;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace EnsembleRoot.Scripts.Assets;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
[GlobalClass]
public partial class AssetHandle : RigidBody3D
{
	// An eighth of water's, as blocks are hollow shells like toy bricks, until materials have their own
	public const float DefaultDensity = 1000 / 8f;

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

	public PlotHandle Plot { get; private set; } = null!;
	public GdInstance Instance { get; private set; } = null!;

	/// <summary>The assembly carrying this handle while its plot is spawned, set for the handle's whole life.</summary>
	public Assembly? Assembly { get; internal set; }

	public bool IsLive => Assembly is not null;

	public IReadOnlyList<AssetComponent> Components { get; private set; } = [];

	public TComponent? GetComponent<TComponent>() where TComponent : AssetComponent =>
		Components.OfType<TComponent>().FirstOrDefault();

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

	/// <summary>
	///     Binds the handle to its plot and instance, then initializes its components and itself, once every handle of
	///     its creation exists. Previews and other unbound handles are never initialized.
	/// </summary>
	internal void Initialize(PlotHandle plot, GdInstance instance)
	{
		Plot = plot;
		Instance = instance;
		Components = [.. GetChildren().OfType<AssetComponent>()];

		foreach (var component in Components)
			component.Initialize(this);

		OnInitialized();
	}

	protected virtual void OnInitialized() { }

	private Aabb CalculateBoundary() =>
		GetColliderBounds() is [var first, .. var rest]
			? rest.Aggregate(first, static (sum, aabb) => sum.Merge(aabb))
			: default;
}
