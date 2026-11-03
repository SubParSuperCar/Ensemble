using EnsembleRoot.Scripts.Plots.Placement;
using Godot;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class PlacementSolverTests
{
	private static readonly Aabb UnitCube = new(-Vector3.One / 2, Vector3.One);

	private readonly PlacementSolver _solver = new();

	[Fact]
	public void Settle_BelowFloor_RaisesOntoIt()
	{
		var box = _solver.Settle(Cube(new Vector3(3, -2, 1)), new ObbGrid(), 0f, Vector3.Up, null);
		Assert.Equal(new Vector3(3, 0.5f, 1), box.Center);
	}

	[Fact]
	public void Settle_IntoObstacle_PushesOutAlongNormal()
	{
		var obstacles = new ObbGrid();
		obstacles.Add(0, Cube(new Vector3(0, 0.5f, 0)));

		var box = _solver.Settle(Cube(new Vector3(0.75f, 0.5f, 0)), obstacles, 0f, Vector3.Right, null);

		Assert.True(box.Center.X >= 1f - 1e-4f);
		Assert.Equal(0.5f, box.Center.Y, 1e-4f);
	}

	[Fact]
	public void Settle_WhileSnapping_AlignsEdgesToGrid()
	{
		var box = _solver.Settle(Cube(new Vector3(0.3f, 0.5f, -0.2f)), new ObbGrid(), 0f, Vector3.Up, 1f);
		Assert.Equal(new Vector3(0.5f, 0.5f, -0.5f), box.Center);
	}

	// A whole-cell box stays on the plot's grid against any face: it leaves a gap rather than following one off it
	[Theory]
	[InlineData(0.3f, 1f)]
	[InlineData(0.5f, 1f)]
	[InlineData(1f, 1f)]
	public void Settle_AgainstOffGridFace_StaysOnGrid(float face, float expectedEdge)
	{
		var obstacles = new ObbGrid();
		obstacles.Add(0, Slab(new Vector3(0, face / 2, 0), face));

		var box = _solver.Settle(Cube(new Vector3(0.2f, face + 0.5f, -0.1f)), obstacles, 0f, Vector3.Up, 1f);

		Assert.Equal(new Vector3(0.5f, expectedEdge + 0.5f, -0.5f), box.Center);
	}

	[Fact]
	public void Settle_UnderOffGridFace_RoundsAwayFromIt()
	{
		var obstacles = new ObbGrid();
		obstacles.Add(0, Slab(new Vector3(0, 2.25f, 0), 0.5f));

		var box = _solver.Settle(Cube(new Vector3(0.5f, 1.5f, 0.5f)), obstacles, 0f, Vector3.Down, 1f);

		Assert.Equal(1.5f, box.Center.Y);
	}

	// A half slab's far edge finds the grid, so it can sit flush on another half slab and complete the cell
	[Fact]
	public void Settle_ThinBoxAgainstOffGridFace_SnapsNearerEdge()
	{
		var obstacles = new ObbGrid();
		obstacles.Add(0, Slab(new Vector3(0.5f, 0.25f, 0.5f), 0.5f));

		var box = _solver.Settle(Slab(new Vector3(0.5f, 0.75f, 0.5f), 0.5f), obstacles, 0f, Vector3.Up, 1f);

		Assert.Equal(0.75f, box.Center.Y);
	}

	[Fact]
	public void Settle_SameInput_SameResultFromAnyHitOnAFace()
	{
		var obstacles = new ObbGrid();
		obstacles.Add(0, Cube(new Vector3(0.5f, 0.5f, 0.5f)));

		var first = _solver.Settle(Cube(new Vector3(0.1f, 1.5f, 0.1f)), obstacles, 0f, Vector3.Up, 1f);
		var second = _solver.Settle(Cube(new Vector3(0.9f, 1.5f, 0.9f)), obstacles, 0f, Vector3.Up, 1f);

		Assert.Equal(first.Center, second.Center);
	}

	// The Wedge's collider: a hair smaller than its cell, and offset within it, so only some faces weld
	[Theory]
	[InlineData(0f)]
	[InlineData(180f)]
	public void SnapToLattice_InsetCollider_PutsOriginOnGrid(float rollDegrees)
	{
		var bounds = new Aabb(new Vector3(-0.248f, -0.25f, -0.25f), new Vector3(0.498f, 0.498f, 0.5f));
		var basis = new Basis(Vector3.Back, float.DegreesToRadians(rollDegrees));
		var box = Obb.From(bounds, new Transform3D(basis, new Vector3(1.1f, 0.25f, 0.2f)));

		box = _solver.Settle(box, new ObbGrid(), 0f, Vector3.Up, 0.5f);
		var origin = PlacementSolver.SnapToLattice(box.Center - basis * bounds.GetCenter(), 0.5f);

		Assert.Equal(new Vector3(1.25f, 0.25f, 0.25f), origin);
	}

	[Fact]
	public void SnapToLattice_BeyondTolerance_KeepsPoint()
	{
		var point = new Vector3(0.26f, 0.3f, -0.02f);
		Assert.Equal(point, PlacementSolver.SnapToLattice(point, 0.5f));
	}

	private static Obb Cube(Vector3 center) => Obb.From(UnitCube, new Transform3D(Basis.Identity, center));

	private static Obb Slab(Vector3 center, float height) =>
		new(center, Basis.Identity, new Vector3(0.5f, height / 2, 0.5f));
}
