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
		var candidateIds = new List<int>();

		for (var i = 0; i < BoxCount; i++)
		{
			var probe = RandomCube(random);
			grid.Query(probe, candidates);
			grid.Query(probe, candidateIds);

			Assert.Equal(candidates, candidateIds.Select(id => boxes[id]));

			// Boxes spanning several cells must still be reported once
			Assert.Equal(candidates.Count, candidates.Distinct().Count());
			Assert.All(boxes.Values.Where(probe.Overlaps), box => Assert.Contains(box, candidates));
			Assert.Equal(boxes.Values.Any(probe.Overlaps), grid.Overlaps(probe));
		}
	}

	[Fact]
	public void AddAndQuery_WithHugeFarOrNonFiniteBoxes_StayBoundedAndReportThem()
	{
		var grid = new ObbGrid();
		var huge = Obb.From(new Aabb(-Vector3.One * 1e30f, Vector3.One * 2e30f), Transform3D.Identity);
		var far = Cube(Vector3.One * 3e38f, Basis.Identity);
		var nonFinite = Cube(Vector3.One * float.NaN, Basis.Identity);
		var near = Cube(Vector3.Zero, Basis.Identity);
		var candidates = new List<Obb>();

		grid.Add(0, huge);
		grid.Add(1, far);
		grid.Add(2, nonFinite);
		grid.Add(3, near);

		grid.Query(near, candidates);
		Assert.Contains(huge, candidates);
		Assert.Contains(near, candidates);
		Assert.True(grid.Overlaps(near));

		grid.Query(far, candidates);
		Assert.Equal(4, candidates.Count);

		grid.Remove(0);
		grid.Remove(1);
		grid.Remove(2);
		grid.Query(near, candidates);
		Assert.Equal([near], candidates);
	}

	[Fact]
	public void Clear_RemovesGriddedAndUngriddedBoxes()
	{
		var grid = new ObbGrid();
		var huge = Obb.From(new Aabb(-Vector3.One * 1e30f, Vector3.One * 2e30f), Transform3D.Identity);
		var near = Cube(Vector3.Zero, Basis.Identity);
		var candidates = new List<Obb>();

		grid.Add(0, huge);
		grid.Add(1, near);
		grid.Clear();

		grid.Query(near, candidates);
		Assert.Empty(candidates);
		Assert.False(grid.Overlaps(near));

		grid.Add(1, near);
		Assert.True(grid.Overlaps(near));
	}

	private static Obb RandomCube(Random random) =>
		Cube(RandomPoint(random), new Basis(RandomAxis(random), random.NextSingle() * float.Tau));

	private static Vector3 RandomPoint(Random random) =>
		new(random.NextSingle() * 20 - 10, random.NextSingle() * 20 - 10, random.NextSingle() * 20 - 10);

	private static Vector3 RandomAxis(Random random) =>
		new Vector3(random.NextSingle() - 0.5f, random.NextSingle() - 0.5f, random.NextSingle() + 0.1f).Normalized();

	private static Obb Cube(Vector3 center, Basis basis) => Obb.From(UnitCube, new Transform3D(basis, center));
}
