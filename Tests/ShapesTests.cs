using EnsembleRoot.Scripts.Assets.Impl;
using Godot;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class ShapesTests
{
	[Fact]
	public void Measure_GetsVolumeAndCentroidOfClosedTriangles_AwayFromTheOrigin()
	{
		var offset = new Vector3(2, 0, 0);
		var (o, x, y, z) = (offset, offset + Vector3.Right, offset + Vector3.Up, offset + Vector3.Back);

		var (volume, centroid) = Shapes.Measure([o, y, x, o, x, z, o, z, y, x, y, z]);

		Assert.Equal(1 / 6f, volume, 1e-6f);
		Assert.True(centroid.IsEqualApprox(offset + Vector3.One / 4));
	}
}
