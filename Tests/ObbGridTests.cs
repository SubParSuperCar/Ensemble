using EnsembleRoot.Scripts.Plots.Impl;
using Godot;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class ObbGridTests
{
	private static readonly Aabb UnitCube = new(-Vector3.One / 2, Vector3.One);

	[Fact]
	public void Overlaps_MatchesBruteForce()
	{
		var random = new Random(1);
		var grid = new ObbGrid();
		var boxes = new List<Obb>();

		for (var id = 0; id < 500; id++)
		{
			var box = RandomCube(random);
			grid.Add(id, box);
			boxes.Add(box);
		}

		for (var i = 0; i < 500; i++)
		{
			var probe = RandomCube(random);
			Assert.Equal(boxes.Exists(probe.Overlaps), grid.Overlaps(probe));
		}
	}

	[Fact]
	public void Remove_StopsReportingBox()
	{
		var grid = new ObbGrid();
		grid.Add(0, Cube(Vector3.Zero, Basis.Identity));

		grid.Remove(0);

		Assert.False(grid.Overlaps(Cube(Vector3.Zero, Basis.Identity)));
		Assert.Equal(0, grid.Count);
	}

	[Fact]
	public void Query_ReturnsSpanningBoxOnce()
	{
		var grid = new ObbGrid();
		var results = new List<Obb>();
		grid.Add(0, Obb.From(new Aabb(Vector3.Zero, Vector3.One * 9), Transform3D.Identity));

		grid.Query(Cube(Vector3.One * 4, Basis.Identity), results);

		Assert.Single(results);
	}

	private static Obb RandomCube(Random random) =>
		Cube(RandomPoint(random), new Basis(RandomAxis(random), random.NextSingle() * Mathf.Tau));

	private static Vector3 RandomPoint(Random random) =>
		new(random.NextSingle() * 20 - 10, random.NextSingle() * 20 - 10, random.NextSingle() * 20 - 10);

	private static Vector3 RandomAxis(Random random) =>
		new Vector3(random.NextSingle() - 0.5f, random.NextSingle() - 0.5f, random.NextSingle() + 0.1f).Normalized();

	private static Obb Cube(Vector3 center, Basis basis) => Obb.From(UnitCube, new Transform3D(basis, center));
}
