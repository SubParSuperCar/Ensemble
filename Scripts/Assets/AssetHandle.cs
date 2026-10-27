using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Scripts.Creations;
using EnsembleRoot.Scripts.Plots;
using Godot;
using Serilog;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Assets;

/// <summary>
///     The root of an asset's scene, and of each placed instance of it. Its direct colliders form the asset's first
///     piece, and <see cref="AssetPiece" /> children form the others; joints between pieces are Godot joints, also
///     direct children, which only link bodies while the creation is live.
/// </summary>
[GlobalClass]
public partial class AssetHandle : StaticBody3D
{
	// An eighth of water's, as blocks are hollow shells like toy bricks
	public const float DefaultDensity = 1000 / 8f;

	private bool _isBoundaryCalculated;

	// Where each piece sits in the handle's space, as built: a live creation moves the pieces into their assemblies
	private Transform3D[] _pieceTransforms = [];

	[Export(PropertyHint.Range, "0,0,1,or_greater,hide_slider")]
	public int AssetId { get; set; }

	[Export] public StringName AssetName { get; set; } = null!;

	// Shown when hovering over the asset in the selector; test assets may go without
	[Export] public StringName? Description { get; set; }

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

	// ReSharper disable once MemberCanBeProtected.Global
	public bool IsLive => Assembly is not null;

	/// <summary>The asset's other pieces, in scene order, so piece <c>i</c> (from 1) is <c>Pieces[i - 1]</c>.</summary>
	public IReadOnlyList<AssetPiece> Pieces { get; private set; } = [];

	public int PieceCount => Pieces.Count + 1;

	/// <summary>The asset's joints, each with the pieces it links, or null where a node is neither.</summary>
	public IReadOnlyList<(Joint3D Joint, int? PieceA, int? PieceB)> Joints { get; private set; } = [];

	public IReadOnlyList<AssetComponent> Components { get; private set; } = [];

	public override void _Notification(int what)
	{
		if (what == NotificationSceneInstantiated)
			CollectPieces();
	}

	// ReSharper disable once UnusedMember.Global
	public TComponent? GetComponent<TComponent>() where TComponent : AssetComponent =>
		Components.OfType<TComponent>().FirstOrDefault();

	/// <summary>
	///     Gets a piece's colliders, with their transforms in the handle's space; piece 0 is the handle's own.
	/// </summary>
	public IEnumerable<(CollisionShape3D Collider, Transform3D Transform)> GetColliders(int piece)
	{
		Node3D body = piece is 0 ? this : Pieces[piece - 1];
		var offset = piece is 0 ? Transform3D.Identity : _pieceTransforms[piece - 1];

		// Only direct children count, as a body only registers the shapes of its direct children
		return body.GetChildren()
			.OfType<CollisionShape3D>()
			.Where(static collider => collider is { Shape: not null, Disabled: false })
			.Select(collider => (collider, offset * collider.Transform));
	}

	public Aabb[] GetColliderBounds(int piece) =>
		[.. GetColliders(piece).Select(static pair => pair.Transform * pair.Collider.Shape.GetDebugMesh().GetAabb())];

	/// <summary>Gets a piece's combined collider volume and centroid, from which its mass follows.</summary>
	public (float Volume, Vector3 Centroid) CalculateVolume(int piece)
	{
		var volume = 0f;
		var moment = Vector3.Zero;

		foreach (var (collider, transform) in GetColliders(piece))
		{
			var (shapeVolume, centroid) = Shapes.Measure(collider.Shape);
			var scaledVolume = shapeVolume * MathF.Abs(transform.Basis.Determinant());

			volume += scaledVolume;
			moment += transform * centroid * scaledVolume;
		}

		return volume > 0 ? (volume, moment / volume) : default;
	}

	/// <summary>Puts the handle's body and its pieces' bodies on a collision layer and mask.</summary>
	public void SetCollision(uint layer, uint mask)
	{
		CollisionLayer = layer;
		CollisionMask = mask;

		foreach (var piece in Pieces)
		{
			piece.CollisionLayer = layer;
			piece.CollisionMask = mask;
		}
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

	/// <summary>
	///     Links each joint to its pieces' assemblies, once every assembly of the creation is in the tree.
	/// </summary>
	internal void ConnectJoints()
	{
		foreach (var (joint, pieceA, pieceB) in Joints)
		{
			if (GetAssembly(pieceA) is not { } bodyA || GetAssembly(pieceB) is not { } bodyB)
				continue;

			if (bodyA == bodyB)
			{
				Log.Warning(
					"Skipped joint {Joint} of instance {InstanceId}, as welds put both of its pieces in one assembly",
					joint.Name, InstanceId);

				continue;
			}

			joint.NodeA = joint.GetPathTo(bodyA);
			joint.NodeB = joint.GetPathTo(bodyB);
		}
	}

	protected virtual void OnInitialized() { }

	/// <summary>
	///     Reads a numeric property of the instance, clamped to what the asset supports: properties take any value of
	///     their type, so each asset enforces its own limits where it uses them.
	/// </summary>
	protected float GetNumberProperty(string key, float min, float max) =>
		Math.Clamp((float)Instance.Properties.GetValue(key).AsDouble(), min, max);

	// Before anything moves the pieces; the joints are unlinked until live, so no build or preview links bodies
	private void CollectPieces()
	{
		Pieces = [.. GetChildren().OfType<AssetPiece>()];
		_pieceTransforms = [.. Pieces.Select(static piece => piece.Transform)];

		for (var index = 0; index < Pieces.Count; index++)
		{
			Pieces[index].Handle = this;
			Pieces[index].Index = index + 1;
		}

		Joints =
		[
			.. GetChildren()
				.OfType<Joint3D>()
				.Select(joint => (joint, GetPieceIndex(joint, joint.NodeA), GetPieceIndex(joint, joint.NodeB)))
		];

		foreach (var (joint, _, _) in Joints)
			joint.NodeA = joint.NodeB = new NodePath();
	}

	private int? GetPieceIndex(Joint3D joint, NodePath path) =>
		joint.GetNodeOrNull(path) switch
		{
			AssetHandle handle when handle == this => 0,
			AssetPiece piece when piece.Handle == this => piece.Index,
			_ => null
		};

	private Assembly? GetAssembly(int? piece) =>
		piece switch
		{
			0 => Assembly,
			{ } index => Pieces[index - 1].Assembly,
			null => null
		};

	private Aabb CalculateBoundary() =>
		Enumerable.Range(0, PieceCount)
			.SelectMany(GetColliderBounds)
			.Aggregate((Aabb?)null, static (sum, aabb) => sum?.Merge(aabb) ?? aabb) ?? default;
}
