using EnsembleRoot.Scripts.Plots.Impl;
using Godot;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class ObbTests
{
	private static readonly Aabb UnitCube = new(-Vector3.One / 2, Vector3.One);
	private static readonly Basis Yaw45 = new(Vector3.Up, Mathf.Pi / 4);

	[Fact]
	public void Overlaps_IgnoresTouchingFaces()
	{
		var box = Cube(Vector3.Zero);

		Assert.False(box.Overlaps(Cube(Vector3.Right)));
		Assert.False(box.Overlaps(Cube(new Vector3(1, 1, 0))));
	}

	[Fact]
	public void Overlaps_DetectsPenetration() =>
		Assert.True(Cube(Vector3.Zero).Overlaps(Cube(new Vector3(0.75f, 0, 0))));

	[Fact]
	public void Overlaps_DetectsRotatedCorner()
	{
		var box = Cube(Vector3.Zero);

		// A 45-degree cube's corner reaches sqrt(2) / 2 from its center
		Assert.True(box.Overlaps(Cube(new Vector3(1.2f, 0, 0), Yaw45)));
		Assert.False(box.Overlaps(Cube(new Vector3(1.25f, 0, 0), Yaw45)));
	}

	[Fact]
	public void Overlaps_UsesEdgeAxes()
	{
		// Only an edge cross-product axis separates these; face axes alone would report an overlap
		var tilted = new Basis(Vector3.Right, Mathf.Pi / 4) * Yaw45;

		Assert.False(Cube(Vector3.Zero).Overlaps(Cube(new Vector3(1.1f, 1.1f, 0), tilted)));
	}

	[Fact]
	public void GetSeparationDistance_ReachesContact()
	{
		var mover = Cube(new Vector3(0.25f, 0, 0));
		var obstacle = Cube(Vector3.Zero);

		var distance = mover.GetSeparationDistance(obstacle, Vector3.Right);

		Assert.Equal(0.75f, distance, 1e-6f);
		Assert.False(mover.At(mover.Center + Vector3.Right * distance).Overlaps(obstacle));
	}

	[Fact]
	public void GetSeparationDistance_IsZeroWhenApart() =>
		Assert.Equal(0, Cube(new Vector3(3, 0, 0)).GetSeparationDistance(Cube(Vector3.Zero), Vector3.Right));

	[Fact]
	public void GetMinimumTranslation_PushesOutSideways()
	{
		// Resting on the ground beside a block, overlapping it slightly: out to the side, not up onto it
		var mover = Cube(new Vector3(0.8f, 0, 0));

		var translation = mover.GetMinimumTranslation(Cube(Vector3.Zero), Vector3.Up);

		Assert.Equal(0.2f, translation.X, 1e-6f);
		Assert.Equal(0, translation.Y);
		Assert.Equal(0, translation.Z);
	}

	[Fact]
	public void GetMinimumTranslation_NeverPushesIntoSurface()
	{
		// The shortest way out is down, but the surface below forbids it
		var mover = Cube(new Vector3(0, -0.9f, 0));
		var obstacle = Cube(Vector3.Zero);

		var translation = mover.GetMinimumTranslation(obstacle, Vector3.Up);

		Assert.True(translation.Y >= 0);
		Assert.False(mover.At(mover.Center + translation).Overlaps(obstacle));
	}

	private static Obb Cube(Vector3 center, Basis? basis = null) =>
		Obb.From(UnitCube, new Transform3D(basis ?? Basis.Identity, center));
}
