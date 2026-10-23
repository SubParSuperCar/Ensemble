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
		var box = _solver.Settle(Cube(new Vector3(3, -2, 1)), new ObbGrid(), 0f, Vector3.Up, null, Vector3.Zero);
		Assert.Equal(new Vector3(3, 0.5f, 1), box.Center);
	}

	[Fact]
	public void Settle_IntoObstacle_PushesOutAlongNormal()
	{
		var obstacles = new ObbGrid();
		obstacles.Add(0, Cube(new Vector3(0, 0.5f, 0)));

		var box = _solver.Settle(Cube(new Vector3(0.75f, 0.5f, 0)), obstacles, 0f, Vector3.Right, null, Vector3.Zero);

		Assert.True(box.Center.X >= 1f - 1e-4f);
		Assert.Equal(0.5f, box.Center.Y, 1e-4f);
	}

	[Fact]
	public void Settle_WhileSnapping_AlignsEdgesToGrid()
	{
		var box = _solver.Settle(Cube(new Vector3(0.3f, 0.5f, -0.2f)), new ObbGrid(), 0f, Vector3.Up, 1f, Vector3.Zero);
		Assert.Equal(new Vector3(0.5f, 0.5f, -0.5f), box.Center);
	}

	[Theory]
	[InlineData(0.49f, 0.5f)]
	[InlineData(0.75f, 0.75f)]
	public void GetSnapExtent_NearWholeCells_SnapsToWhole(float extent, float expected) =>
		Assert.Equal(expected, PlacementSolver.GetSnapExtent(extent, 1f));

	// The Wedge's collider: a hair smaller than its cell, and offset within it, so only some faces weld
	[Theory]
	[InlineData(0f)]
	[InlineData(180f)]
	public void SnapToLattice_InsetCollider_PutsOriginOnGrid(float rollDegrees)
	{
		var bounds = new Aabb(new Vector3(-0.248f, -0.25f, -0.25f), new Vector3(0.498f, 0.498f, 0.5f));
		var basis = new Basis(Vector3.Back, float.DegreesToRadians(rollDegrees));
		var box = Obb.From(bounds, new Transform3D(basis, new Vector3(1.1f, 0.3f, 0.2f)));

		box = _solver.Settle(box, new ObbGrid(), 0f, Vector3.Up, 0.5f, Vector3.Zero);
		var origin = PlacementSolver.SnapToLattice(box.Center - basis * bounds.GetCenter(), Vector3.Zero, 0.5f);

		Assert.Equal(new Vector3(1.25f, 0.25f, 0.25f), origin);
	}

	[Fact]
	public void SnapToLattice_BeyondTolerance_KeepsPoint()
	{
		var point = new Vector3(0.26f, 0.3f, -0.02f);
		Assert.Equal(point, PlacementSolver.SnapToLattice(point, Vector3.Zero, 0.5f));
	}

	private static Obb Cube(Vector3 center) => Obb.From(UnitCube, new Transform3D(Basis.Identity, center));
}
