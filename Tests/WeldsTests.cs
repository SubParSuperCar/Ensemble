using EnsembleRoot.Scripts.Plots.Impl;
using Godot;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class WeldsTests
{
	private static readonly Aabb UnitCube = new(-Vector3.One / 2, Vector3.One);

	[Fact]
	public void Group_JoinsChainsAcrossFacesOnly_InIdOrderRegardlessOfInputOrder()
	{
		// 2 abuts 0 abuts 5 in a row; 3 only meets 5 at an edge; 7 has no boxes; 4 is apart
		(int Id, Obb[] Boxes)[] parts =
		[
			(5, [Cube(new Vector3(1, 0, 0))]), (3, [Cube(new Vector3(2, 1, 0))]), (7, []),
			(0, [Cube(Vector3.Zero)]), (4, [Cube(new Vector3(9, 0, 0))]), (2, [Cube(new Vector3(-1, 0, 0))])
		];

		Assert.Equal([[0, 2, 5], [3], [4], [7]], Welds.Group(parts));
		Assert.Equal(Welds.Group(parts), Welds.Group([.. parts.Reverse()]));
	}

	[Fact]
	public void Group_WeldsThroughAnyOfAPartsBoxes()
	{
		// Part 1's second box bridges it to part 2, while its first box only touches part 0
		(int Id, Obb[] Boxes)[] parts =
		[
			(0, [Cube(Vector3.Zero)]), (1, [Cube(new Vector3(0, 1, 0)), Cube(new Vector3(0, 1, 2))]),
			(2, [Cube(new Vector3(0, 1, 3))])
		];

		Assert.Equal([[0, 1, 2]], Welds.Group(parts));
	}

	private static Obb Cube(Vector3 center) => Obb.From(UnitCube, new Transform3D(Basis.Identity, center));
}
