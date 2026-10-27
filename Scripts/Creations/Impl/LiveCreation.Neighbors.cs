using Godot;

namespace EnsembleRoot.Scripts.Creations;

public partial class LiveCreation
{
	// Times an assembly's bounding sphere, so creations change hands before they touch rather than as they do
	private const float ArbitrationRadiusScale = 1.5f;

	// Past the creation's bounds, so bodies resting on its surface count as touching it
	private const float WakeMargin = 1 / 8f;

	/// <summary>The spheres arbitration compares with other creations', as of the last update.</summary>
	internal ArbitrationSpheres ArbitrationSpheres { get; } = new();

	/// <summary>Places a sphere around each assembly where it is; proxies are placed without asking Godot.</summary>
	internal void UpdateArbitrationSpheres()
	{
		ArbitrationSpheres.Clear();

		foreach (var assembly in _assemblies)
		{
			var bounds = assembly.Bounds;

			// Without colliders, it has nothing to collide with
			if (assembly.IsKilled || bounds.Size == Vector3.Zero)
				continue;

			var transform = assembly.IsSimulated ? assembly.GlobalTransform : assembly.ProxyTransform;
			ArbitrationSpheres.Add(transform * bounds.GetCenter(), bounds.Size.Length() / 2 * ArbitrationRadiusScale);
		}
	}

	/// <summary>
	///     Wakes this peer's sleeping bodies that touch the bounds, as something they rest on may be leaving.
	/// </summary>
	internal void WakeAssemblies(Aabb bounds)
	{
		foreach (var assembly in _assemblies)
			if (assembly is { IsSimulated: true, IsKilled: false, Sleeping: true } &&
				(assembly.GlobalTransform * assembly.Bounds).Intersects(bounds))
				assembly.Sleeping = false;
	}

	/// <summary>
	///     Wakes other creations' bodies touching this one before it despawns, as Jolt leaves bodies asleep when what
	///     they rest on leaves the space, and they'd hang in the air.
	/// </summary>
	internal void WakeNeighbors()
	{
		var bounds = _assemblies
			.Where(static assembly => !assembly.IsKilled)
			.Select(static assembly => assembly.GlobalTransform * assembly.Bounds)
			.Aggregate((Aabb?)null, static (sum, aabb) => sum?.Merge(aabb) ?? aabb);

		if (bounds is { } creationBounds)
			GPlotManager.WakeAssembliesNear(creationBounds.Grow(WakeMargin), this);
	}
}
