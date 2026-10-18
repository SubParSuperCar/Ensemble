using Godot;

namespace EnsembleRoot.Scripts.Assets.Impl;

public static class Shapes
{
	/// <summary>Gets a shape's volume and centroid: exact for primitives, from its debug mesh for the rest.</summary>
	public static (float Volume, Vector3 Centroid) Measure(Shape3D shape) =>
		shape switch
		{
			BoxShape3D box => (box.Size.X * box.Size.Y * box.Size.Z, Vector3.Zero),
			SphereShape3D sphere => (GetBallVolume(sphere.Radius), Vector3.Zero),
			CylinderShape3D cylinder => (GetDiscArea(cylinder.Radius) * cylinder.Height, Vector3.Zero),
			CapsuleShape3D capsule => (
				GetDiscArea(capsule.Radius) * (capsule.Height - 2 * capsule.Radius) + GetBallVolume(capsule.Radius),
				Vector3.Zero),
			_ => Measure(shape.GetDebugMesh())
		};

	/// <summary>Gets the volume and centroid enclosed by a closed triangle list, from signed tetrahedra.</summary>
	public static (float Volume, Vector3 Centroid) Measure(ReadOnlySpan<Vector3> triangles)
	{
		var volume = 0f;
		var moment = Vector3.Zero;

		for (var i = 0; i + 2 < triangles.Length; i += 3)
		{
			var (a, b, c) = (triangles[i], triangles[i + 1], triangles[i + 2]);
			var tetrahedron = a.Dot(b.Cross(c)) / 6;

			volume += tetrahedron;
			moment += (a + b + c) / 4 * tetrahedron;
		}

		return volume is 0 ? default : (MathF.Abs(volume), moment / volume);
	}

	private static (float Volume, Vector3 Centroid) Measure(ArrayMesh mesh)
	{
		for (var surface = 0; surface < mesh.GetSurfaceCount(); surface++)
		{
			if (mesh.SurfaceGetPrimitiveType(surface) is not Mesh.PrimitiveType.Triangles)
				continue;

			var arrays = mesh.SurfaceGetArrays(surface);
			var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
			var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();

			return Measure(indices.Length > 0 ? [.. indices.Select(index => vertices[index])] : vertices);
		}

		return default;
	}

	private static float GetBallVolume(float radius) => 4 / 3f * MathF.PI * radius * radius * radius;
	private static float GetDiscArea(float radius) => MathF.PI * radius * radius;
}
