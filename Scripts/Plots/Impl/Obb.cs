using System.Runtime.InteropServices;
using Godot;

namespace EnsembleRoot.Scripts.Plots.Impl;

/// <summary>
///     An oriented bounding box tested with the Separating Axis Theorem: two boxes are disjoint if their projections
///     are disjoint on any of 15 axes, i.e., each box's 3 face normals and the 9 cross products of their edges.
///     Touching boxes do not overlap.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct Obb(Vector3 Center, Basis Basis, Vector3 Extents)
{
	public Vector3 Envelope => new(Radius(Vector3.Right), Radius(Vector3.Up), Radius(Vector3.Back));

	public static Obb From(Aabb bounds, Transform3D transform) =>
		new(transform * bounds.GetCenter(), transform.Basis, bounds.Size / 2);

	public Obb At(Vector3 center) => this with { Center = center };

	public float Radius(Vector3 direction) =>
		Extents.X * MathF.Abs(Basis.X.Dot(direction)) +
		Extents.Y * MathF.Abs(Basis.Y.Dot(direction)) +
		Extents.Z * MathF.Abs(Basis.Z.Dot(direction));

	public bool Overlaps(Obb other)
	{
		var reach = Extents.Length() + other.Extents.Length();

		if ((other.Center - Center).LengthSquared() >= reach * reach)
			return false;

		if (IsSeparatedOn(other, Vector3.Right) || IsSeparatedOn(other, Vector3.Up) ||
			IsSeparatedOn(other, Vector3.Back))
			return false;

		// ReSharper disable once LoopCanBeConvertedToQuery
		foreach (var axis in GetAxes(other))
			if (IsSeparatedOn(other, axis))
				return false;

		return true;
	}

	/// <summary>
	///     Gets how far this box must travel along the unit <paramref name="direction" /> to stop overlapping
	///     <paramref name="obstacle" />. Separation on an axis only grows once reached, so the nearest one decides.
	/// </summary>
	public float GetSeparationDistance(Obb obstacle, Vector3 direction)
	{
		var distance = float.PositiveInfinity;

		foreach (var axis in GetAxes(obstacle))
		{
			if (IsSeparatedOn(obstacle, axis))
				return 0;

			var rate = direction.Dot(axis);

			if (Mathf.IsZeroApprox(rate))
				continue;

			var offset = (obstacle.Center - Center).Dot(axis);
			var reach = Radius(axis) + obstacle.Radius(axis);

			distance = MathF.Min(distance, (rate > 0 ? offset + reach : offset - reach) / rate);
		}

		return distance;
	}

	/// <summary>
	///     Gets the shortest translation that separates this box from <paramref name="obstacle" /> without moving it
	///     against the unit <paramref name="normal" />, or along the normal if every shorter way is against it.
	/// </summary>
	public Vector3 GetMinimumTranslation(Obb obstacle, Vector3 normal)
	{
		var translation = Vector3.Zero;
		var shortest = float.PositiveInfinity;

		foreach (var axis in GetAxes(obstacle))
		{
			if (IsSeparatedOn(obstacle, axis))
				return Vector3.Zero;

			var offset = (Center - obstacle.Center).Dot(axis);
			var penetration = Radius(axis) + obstacle.Radius(axis) - MathF.Abs(offset);
			var sign = Mathf.IsZeroApprox(offset) ? axis.Dot(normal) >= 0 ? 1 : -1 : MathF.Sign(offset);
			var push = axis * (sign * penetration);

			if ((push.Dot(normal) < 0 && !Mathf.IsZeroApprox(push.Dot(normal))) || penetration >= shortest)
				continue;

			shortest = penetration;
			translation = push;
		}

		if (!float.IsPositiveInfinity(shortest))
			return translation;

		var distance = GetSeparationDistance(obstacle, normal);
		return float.IsFinite(distance) ? normal * distance : Vector3.Zero;
	}

	private bool IsSeparatedOn(Obb other, Vector3 axis)
	{
		var distance = MathF.Abs((other.Center - Center).Dot(axis));
		var reach = Radius(axis) + other.Radius(axis);

		return distance >= reach || Mathf.IsEqualApprox(distance, reach);
	}

	private IEnumerable<Vector3> GetAxes(Obb other)
	{
		Vector3[] axes = [Basis.X, Basis.Y, Basis.Z];
		Vector3[] otherAxes = [other.Basis.X, other.Basis.Y, other.Basis.Z];

		foreach (var axis in axes.Concat(otherAxes))
			yield return axis;

		foreach (var axis in axes)
			foreach (var otherAxis in otherAxes)
				if (axis.Cross(otherAxis) is var cross && !Mathf.IsZeroApprox(cross.LengthSquared()))
					yield return cross.Normalized();
	}
}
