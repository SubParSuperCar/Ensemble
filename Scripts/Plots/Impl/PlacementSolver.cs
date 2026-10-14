using Godot;

namespace EnsembleRoot.Scripts.Plots.Impl;

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
///     </list>
///     Snapping can land the box in another obstacle, so the steps repeat; a box still overlapping is left for
///     validation to reject.
/// </remarks>
public sealed class PlacementSolver
{
	private const int MaxSettlePasses = 3;
	private const int MaxResolvePasses = 3;
	private const float SnapTolerance = 1 / 32f;

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

	public static float GetSnapExtent(float extent, float cellSize)
	{
		var cells = 2 * extent / cellSize;
		var whole = MathF.Round(cells, MidpointRounding.AwayFromZero);

		return whole > 0 && MathF.Abs(cells - whole) <= SnapTolerance ? whole * cellSize / 2 : extent;
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
