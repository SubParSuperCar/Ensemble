using EnsembleRoot.Scripts.Creations;
using EnsembleRoot.Scripts.Plots.Placement;
using Godot;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class WeldsTests
{
	private static readonly Aabb UnitCube = new(-Vector3.One / 2, Vector3.One);

	[Fact]
	public void Group_JoinsChainsAcrossFacesOnly_InPartOrder()
	{
		// 0 abuts 1 abuts 2 in a row; 3 only meets 2 at an edge; 4 is apart; 5 has no boxes
		(int Owner, Obb[] Boxes)[] parts =
		[
			(10, [Cube(new Vector3(-1, 0, 0))]), (11, [Cube(Vector3.Zero)]), (12, [Cube(new Vector3(1, 0, 0))]),
			(13, [Cube(new Vector3(2, 1, 0))]), (14, [Cube(new Vector3(9, 0, 0))]), (15, [])
		];

		Assert.Equal([[0, 1, 2], [3], [4], [5]], Welds.Group(parts));
	}

	[Fact]
	public void Group_WeldsThroughAnyOfAPartsBoxes()
	{
		// Part 1's second box bridges it to part 2, while its first box only touches part 0
		(int Owner, Obb[] Boxes)[] parts =
		[
			(0, [Cube(Vector3.Zero)]), (1, [Cube(new Vector3(0, 1, 0)), Cube(new Vector3(0, 1, 2))]),
			(2, [Cube(new Vector3(0, 1, 3))])
		];

		Assert.Equal([[0, 1, 2]], Welds.Group(parts));
	}

	[Fact]
	public void Group_NeverWeldsAnOwnersParts_ButOthersMayJoinThem()
	{
		// An instance's two pieces, stacked, with a neighbor beside the lower one
		List<(int Owner, Obb[] Boxes)> parts =
			[(7, [Cube(Vector3.Zero)]), (7, [Cube(new Vector3(0, 1, 0))]), (8, [Cube(new Vector3(1, 0, 0))])];

		Assert.Equal([[0, 2], [1]], Welds.Group(parts));

		// A part abutting both the neighbor and the upper piece closes the loop
		parts.Add((9, [Cube(new Vector3(1, 1, 0))]));

		Assert.Equal([[0, 1, 2, 3]], Welds.Group(parts));
	}

	[Fact]
	public void Group_MatchesPairwiseAbutting_ForRandomRotatedAndOversizedBoxes()
	{
		var random = new Random(11);

		for (var round = 0; round < 64; round++)
		{
			var parts = new List<(int Owner, Obb[] Boxes)>();

			for (var i = random.Next(2, 40); i > 0; i--)
			{
				// Every fifth round has boxes longer than a grid cell
				var boxes = new Obb[random.Next(3)];

				for (var box = 0; box < boxes.Length; box++)
					boxes[box] = RandomBox(random, round % 5 is 0);

				// Some owners have several parts, as multi-piece instances do
				parts.Add((random.Next(parts.Count / 2 + 1), boxes));
			}

			Assert.Equal(GroupPairwise(parts), Welds.Group(parts));
		}
	}

	private static Obb Cube(Vector3 center) => Obb.From(UnitCube, new Transform3D(Basis.Identity, center));

	// Half the boxes sit on the half-unit grid, as placed ones do, so many of them abut
	private static Obb RandomBox(Random random, bool isOversized)
	{
		var extents = isOversized
			? new Vector3(20 + random.NextSingle() * 30, 1, 2)
			: new Vector3(1 / 4f + random.NextSingle(), 1 / 4f + random.NextSingle(), 1 / 4f + random.NextSingle());

		var basis = random.Next(3) switch
		{
			0 => Basis.Identity,
			1 => new Basis(Vector3.Up, random.Next(4) * float.Pi / 2),
			_ => new Basis(RandomUnit(random), random.NextSingle() * float.Tau)
		};

		var center = random.Next(2) is 0
			? new Vector3(random.Next(-8, 9), random.Next(5), random.Next(-8, 9)) / 2
			: new Vector3(random.NextSingle() * 16 - 8, random.NextSingle() * 4, random.NextSingle() * 16 - 8);

		return new Obb(center, basis, extents);
	}

	private static Vector3 RandomUnit(Random random)
	{
		Vector3 vector;

		do
			vector = new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle()) - Vector3.One / 2;
		while (vector.LengthSquared() < 1 / 1024f);

		return vector.Normalized();
	}

	// The reference: every pair of parts of different owners tested directly, without the grid
	private static List<int[]> GroupPairwise(List<(int Owner, Obb[] Boxes)> parts)
	{
		var parents = Enumerable.Range(0, parts.Count).ToArray();

		for (var i = 0; i < parts.Count; i++)
			for (var j = i + 1; j < parts.Count; j++)
				if (parts[i].Owner != parts[j].Owner && parts[i].Boxes.Any(box => parts[j].Boxes.Any(box.Abuts)))
					parents[Find(j)] = Find(i);

		return
		[
			.. Enumerable.Range(0, parts.Count)
				.GroupBy(Find)
				.Select(static group => group.Order().ToArray())
				.OrderBy(static group => group[0])
		];

		int Find(int index)
		{
			while (parents[index] != index)
				index = parents[index];

			return index;
		}
	}
}
