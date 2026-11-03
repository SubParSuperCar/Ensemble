using Godot;

namespace EnsembleRoot.Scripts.Plots.Placement;

/// <summary>Moves a box resting against a surface to the nearest free spot, on the plot's grid while snapping.</summary>
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
///             Snap: per axis, whichever box edge lands nearer a line of the plot's grid wins, so thin boxes can share
///             a cell, and a box spanning whole cells is on the grid whatever it touches. Edges round away from the
///             surface and from the obstacles Resolve pushed out of. Spans within <see cref="SnapTolerance" /> of
///             whole cells snap as exactly whole, centering their slack.
///         </item>
///         <item>Raise: the box is lifted onto the plot floor if below it.</item>
///         <item>
///             Origin (the caller): <see cref="SnapToLattice" /> rounds the asset's origin, worked back from the box,
///             onto the half-cell lattice when within <see cref="LatticeTolerance" /> of it.
///         </item>
///     </list>
///     Snapping can land the box in another obstacle, so the steps repeat; a box still overlapping is left for
///     validation to reject. The result depends only on the hit, the box, the plot's boxes, and the cell size.
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
	public Obb Settle(Obb box, ObbGrid obstacles, float floor, Vector3 normal, float? cellSize)
	{
		for (var pass = 0; pass < MaxSettlePasses; pass++)
		{
			var resolved = Resolve(box, obstacles, normal);
			resolved = Snap(resolved, cellSize, normal, resolved.Center - box.Center);

			box = RaiseTo(resolved, floor);

			if (!obstacles.Overlaps(box))
				break;
		}

		return box;
	}

	/// <summary>
	///     Rounds a point onto the plot's lattice of half <paramref name="cellSize" /> steps, per axis, where it lies
	///     within <see cref="LatticeTolerance" />. Snapping aligns colliders' boxes, so a collider inset or offset by a
	///     hair (e.g., to keep a face from welding) would otherwise carry its asset's origin, and with it the meshes, off
	///     the grid by as much.
	/// </summary>
	public static Vector3 SnapToLattice(Vector3 point, float cellSize)
	{
		var step = cellSize / 2;

		for (var axis = 0; axis < 3; axis++)
		{
			var nearest = MathF.Round(point[axis] / step, MidpointRounding.AwayFromZero) * step;

			if (MathF.Abs(point[axis] - nearest) <= LatticeTolerance)
				point[axis] = nearest;
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

	// Aligns whichever edge keeps the box nearest on every axis, never rounding into the surface or back against a push
	private static Obb Snap(Obb box, float? cellSize, Vector3 normal, Vector3 push)
	{
		if (cellSize is not { } size)
			return box;

		var center = box.Center;
		var envelope = box.Envelope;

		for (var axis = 0; axis < 3; axis++)
		{
			var direction = MathF.Sign(Mathf.IsZeroApprox(push[axis]) ? normal[axis] : push[axis]);
			var extent = GetSnapExtent(envelope[axis], size);
			var tolerance = Epsilon + MathF.Abs(extent - envelope[axis]);
			var low = SnapEdge(center[axis] - extent, size, direction, tolerance) + extent;
			var high = SnapEdge(center[axis] + extent, size, direction, tolerance) - extent;

			center[axis] = MathF.Abs(low - center[axis]) <= MathF.Abs(high - center[axis]) ? low : high;
		}

		return box.At(center);
	}

	private static float GetSnapExtent(float extent, float cellSize)
	{
		var cells = 2 * extent / cellSize;
		var whole = MathF.Round(cells, MidpointRounding.AwayFromZero);

		return whole > 0 && MathF.Abs(cells - whole) <= SnapTolerance ? whole * cellSize / 2 : extent;
	}

	// An edge within the tolerance of a line is on it, as rounding past that line would leave a whole cell's gap
	private static float SnapEdge(float edge, float cellSize, int direction, float tolerance)
	{
		var cells = edge / cellSize;
		var nearest = MathF.Round(cells, MidpointRounding.AwayFromZero);

		if (direction is 0 || MathF.Abs(edge - nearest * cellSize) <= tolerance)
			return nearest * cellSize;

		return (direction > 0 ? MathF.Ceiling(cells) : MathF.Floor(cells)) * cellSize;
	}

	private static Obb RaiseTo(Obb box, float floor)
	{
		var depth = floor - (box.Center.Y - box.Envelope.Y);
		return depth > 0 && !Mathf.IsZeroApprox(depth) ? box.At(box.Center + Vector3.Up * depth) : box;
	}
}
