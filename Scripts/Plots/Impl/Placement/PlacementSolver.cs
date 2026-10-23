using Godot;

namespace EnsembleRoot.Scripts.Plots.Placement;

/// <summary>Moves a box resting against a surface to the nearest free spot, grid-aligned while snapping.</summary>
/// <remarks>
///     The placement pipeline, in the plot's local space:
///     <list type="number">
///         <item>
///             Contact (the caller): while snapping, the hit normal is rounded to its dominant grid axis, so a curved
///             surface steeper than 45 degrees counts as a side. The box is pushed out along the normal by its
///             projected radius, just touching the hit point's tangent plane.
///         </item>
///         <item>
///             Resolve: each overlapping obstacle pushes the box out by its SAT minimum translation, never against the
///             normal (see <see cref="Obb.GetMinimumTranslation" />).
///         </item>
///         <item>
///             Snap: per axis, whichever box edge lands nearer a grid line wins, so thin boxes can share a cell. Edges
///             round away from the obstacles Resolve pushed out of. Spans within <see cref="SnapTolerance" /> of whole
///             cells snap as exactly whole, centering their slack. The grid is anchored to the hovered asset, if any.
///         </item>
///         <item>Raise: the box is lifted onto the plot floor if below it.</item>
///         <item>
///             Origin (the caller): the asset's origin, worked back from the box, rounds onto the half-cell lattice when
///             within <see cref="LatticeTolerance" /> of it, as does the anchor (see <see cref="SnapToLattice" />).
///         </item>
///     </list>
///     Snapping can land the box in another obstacle, so the steps repeat; a box still overlapping is left for
///     validation to reject.
/// </remarks>
public sealed class PlacementSolver
{
	private const int MaxSettlePasses = 3;
	private const int MaxResolvePasses = 3;

	// Of a cell (about 16 mm at the plot's): past both faces' insets plus a round collider's tessellation (the
	// hemisphere's hull falls about 4 mm short), well short of the finest real fraction a span uses (the suspension's
	// plates are a fifth)
	private const float SnapTolerance = 1 / 32f;

	// Twice the widest collider inset (about 4 mm): insets shift an origin off the lattice by at most half of one, and
	// any real asset's off-lattice placement is centimeters
	private const float LatticeTolerance = 2 * MaxColliderInset;

	private readonly List<Obb> _candidates = [];

	// A null cell size skips snapping
	public Obb Settle(Obb box, ObbGrid obstacles, float floor, Vector3 normal, float? cellSize, Vector3 anchor)
	{
		for (var pass = 0; pass < MaxSettlePasses; pass++)
		{
			var resolved = Resolve(box, obstacles, normal);
			resolved = Snap(resolved, cellSize, anchor, resolved.Center - box.Center);

			box = RaiseTo(resolved, floor);

			if (!obstacles.Overlaps(box))
				break;
		}

		return box;
	}

	public static float GetSnapExtent(float extent, float cellSize)
	{
		var cells = 2 * extent / cellSize;
		var whole = MathF.Round(cells, MidpointRounding.AwayFromZero);

		return whole > 0 && MathF.Abs(cells - whole) <= SnapTolerance ? whole * cellSize / 2 : extent;
	}

	/// <summary>
	///     Rounds a point onto the lattice of half <paramref name="cellSize" /> steps from <paramref name="anchor" />, per
	///     axis, where it lies within <see cref="LatticeTolerance" />. Snapping aligns colliders' boxes, so a collider
	///     inset or offset by a hair (e.g., to keep a face from welding) would otherwise carry its asset's origin, and
	///     with it the meshes, off the grid by as much.
	/// </summary>
	public static Vector3 SnapToLattice(Vector3 point, Vector3 anchor, float cellSize)
	{
		var step = cellSize / 2;

		for (var axis = 0; axis < 3; axis++)
		{
			var offset = point[axis] - anchor[axis];
			var nearest = MathF.Round(offset / step, MidpointRounding.AwayFromZero) * step;

			if (MathF.Abs(offset - nearest) <= LatticeTolerance)
				point[axis] = anchor[axis] + nearest;
		}

		return point;
	}

	private Obb Resolve(Obb box, ObbGrid obstacles, Vector3 normal)
	{
		for (var pass = 0; pass < MaxResolvePasses; pass++)
		{
			var isMoved = false;
			obstacles.Query(box, _candidates);

			// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
			foreach (var obstacle in _candidates)
			{
				if (!box.Overlaps(obstacle))
					continue;

				var translation = box.GetMinimumTranslation(obstacle, normal);

				if (translation == Vector3.Zero)
					continue;

				box = box.At(box.Center + translation);
				isMoved = true;
			}

			if (!isMoved)
				break;
		}

		return box;
	}

	// Aligns whichever edge keeps the box nearest on every axis, never rounding back against a push
	private static Obb Snap(Obb box, float? cellSize, Vector3 anchor, Vector3 push)
	{
		if (cellSize is not { } size)
			return box;

		var center = box.Center;
		var envelope = box.Envelope;

		for (var axis = 0; axis < 3; axis++)
		{
			var direction = Mathf.IsZeroApprox(push[axis]) ? 0 : MathF.Sign(push[axis]);
			var extent = GetSnapExtent(envelope[axis], size);
			var low = SnapEdge(center[axis] - extent, anchor[axis], size, direction) + extent;
			var high = SnapEdge(center[axis] + extent, anchor[axis], size, direction) - extent;

			center[axis] = MathF.Abs(low - center[axis]) <= MathF.Abs(high - center[axis]) ? low : high;
		}

		return box.At(center);
	}

	private static float SnapEdge(float edge, float anchor, float cellSize, int direction)
	{
		var cells = (edge - anchor) / cellSize;
		var nearest = MathF.Round(cells, MidpointRounding.AwayFromZero);

		var snapped = Mathf.IsEqualApprox(cells, nearest) || direction is 0
			? nearest
			: direction > 0
				? MathF.Ceiling(cells)
				: MathF.Floor(cells);

		return anchor + snapped * cellSize;
	}

	private static Obb RaiseTo(Obb box, float floor)
	{
		var depth = floor - (box.Center.Y - box.Envelope.Y);
		return depth > 0 && !Mathf.IsZeroApprox(depth) ? box.At(box.Center + Vector3.Up * depth) : box;
	}
}
