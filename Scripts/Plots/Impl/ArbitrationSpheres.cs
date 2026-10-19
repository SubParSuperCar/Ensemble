using Godot;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace EnsembleRoot.Scripts.Plots.Impl;

/// <summary>
///     A creation's spheres for arbitration, one per assembly, in world space. Only the spheres within the other
///     creation's bounds are compared, so creations apart cost a bounds test, and overlapping ones stop at the first
///     touching pair.
/// </summary>
public sealed class ArbitrationSpheres
{
	private readonly List<(Vector3 Center, float Radius)> _candidates = [];
	private readonly List<(Vector3 Center, float Radius)> _spheres = [];

	private Aabb _bounds;

	public void Clear() => _spheres.Clear();

	public void Add(Vector3 center, float radius)
	{
		var bounds = new Aabb(center - Vector3.One * radius, Vector3.One * radius * 2);

		_bounds = _spheres.Count is 0 ? bounds : _bounds.Merge(bounds);
		_spheres.Add((center, radius));
	}

	public bool Overlaps(ArbitrationSpheres other)
	{
		if (_spheres.Count is 0 || other._spheres.Count is 0 || !_bounds.Intersects(other._bounds))
			return false;

		_candidates.Clear();

		foreach (var sphere in other._spheres)
			if (Touches(sphere, _bounds))
				_candidates.Add(sphere);

		foreach (var sphere in _spheres)
			if (Touches(sphere, other._bounds))
				foreach (var (center, radius) in _candidates)
					if (sphere.Center.DistanceSquaredTo(center) < (sphere.Radius + radius) * (sphere.Radius + radius))
						return true;

		return false;
	}

	private static bool Touches((Vector3 Center, float Radius) sphere, Aabb box) =>
		sphere.Center.Clamp(box.Position, box.End).DistanceSquaredTo(sphere.Center) < sphere.Radius * sphere.Radius;
}
