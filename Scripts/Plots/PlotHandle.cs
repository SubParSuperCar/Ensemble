using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Scripts.Plots.Outlines;
using EnsembleRoot.Scripts.Plots.Placement;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Plots;

[GlobalClass]
public partial class PlotHandle : Node3D
{
	private Transform3D? _originTransform;
	private PlotOutline? _outline;
	private GdPlot _plot = null!;

	[Export(PropertyHint.Range, "0,0,1,or_greater,hide_slider")]
	public int Id { get; set; } = None;

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxOccupantCount { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int MaxTotalInstanceCount { get; set; }

	public Transform3D OriginTransform => _originTransform ??= CalculateOriginTransform();

	public Transform3D BoundaryTransform { get; private set; }
	public Vector3 BoundarySize { get; private set; }

	public Vector3 GridBoundarySize { get; private set; }

	public override void _Ready()
	{
		_plot = GPlots.GetPlot(Id)!;

		var boundary = GetNode<CollisionShape3D>("Boundary/Definition");
		BoundaryTransform = boundary.GlobalTransform;
		BoundarySize = boundary.Shape.GetDebugMesh().GetAabb().Size;

		GridBoundarySize = BoundarySize / GridToWorldScale;

		ReadyInstances();
		ReadySimulation();
		ReadySpawning();
	}

	public override void _ExitTree()
	{
		ExitSpawning();
		ExitSimulation();
		ExitInstances();
	}

	public override void _PhysicsProcess(double delta)
	{
		WatchSimulator(delta);
		UpdateInput(delta);
	}

	public Vector3 WorldToGrid(Vector3 worldPosition) =>
		OriginTransform.AffineInverse() * worldPosition / GridToWorldScale;

	public Transform3D GridToWorld(Vector3 gridPosition, Quaternion rotation) =>
		OriginTransform * new Transform3D(new Basis(rotation), gridPosition * GridToWorldScale);

	public void SetOutline(Color? color, bool hasMarchingAnts)
	{
		if (color is not { } tint)
		{
			if (IsInstanceValid(_outline))
				_outline.Lower();

			return;
		}

		if (!IsInstanceValid(_outline) || _outline.IsQueuedForDeletion())
		{
			_outline = new PlotOutline
			{
				Name = "Outline",
				Bounds = PlotPlacement.GetBounds(this),
				Transform = GlobalTransform.AffineInverse() * OriginTransform
			};

			AddChild(_outline);
		}

		_outline.Raise(tint, hasMarchingAnts);
	}

	private Transform3D CalculateOriginTransform()
	{
		var floorCollider = GetNode<CollisionShape3D>("Floor/Collider");

		// The floor's surface, so that what rests on the origin's plane really touches what stands on it
		var aabb = floorCollider.Shape.GetDebugMesh().GetAabb();
		var position = floorCollider.GlobalTransform * (aabb.GetCenter() + Vector3.Up * aabb.Size.Y / 2);

		return new Transform3D(floorCollider.GlobalBasis, position);
	}
}
