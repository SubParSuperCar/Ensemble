using EnsembleRoot.Scripts.Plots.Impl;
using Godot;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class ObbGridTests
{
	private const int BoxCount = 500;

	private static readonly Aabb UnitCube = new(-Vector3.One / 2, Vector3.One);

	[Fact]
	public void OverlapsAndQuery_AfterRemovals_MatchBruteForce()
	{
		var random = new Random(1);
		var grid = new ObbGrid();
		var boxes = new Dictionary<int, Obb>();

		for (var id = 0; id < BoxCount; id++)
		{
			var box = RandomCube(random);
			grid.Add(id, box);
			boxes.Add(id, box);
		}

		for (var id = 0; id < BoxCount; id += 2)
		{
			grid.Remove(id);
			boxes.Remove(id);
		}

		var candidates = new List<Obb>();

		for (var i = 0; i < BoxCount; i++)
		{
			var probe = RandomCube(random);
			grid.Query(probe, candidates);

			// Boxes spanning several cells must still be reported once
			Assert.Equal(candidates.Count, candidates.Distinct().Count());
			Assert.Equal(boxes.Values.Any(probe.Overlaps), grid.Overlaps(probe));
		}
	}

	private static Obb RandomCube(Random random) =>
		Cube(RandomPoint(random), new Basis(RandomAxis(random), random.NextSingle() * float.Tau));

	private static Vector3 RandomPoint(Random random) =>
		new(random.NextSingle() * 20 - 10, random.NextSingle() * 20 - 10, random.NextSingle() * 20 - 10);

	private static Vector3 RandomAxis(Random random) =>
		new Vector3(random.NextSingle() - 0.5f, random.NextSingle() - 0.5f, random.NextSingle() + 0.1f).Normalized();

	private static Obb Cube(Vector3 center, Basis basis) => Obb.From(UnitCube, new Transform3D(basis, center));
}
