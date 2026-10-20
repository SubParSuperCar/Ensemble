using System.Runtime.InteropServices;
using EnsembleRoot.Common.Networking;
using Godot;

namespace EnsembleRoot.Scripts.Creations.Packets;

/// <summary>An assembly's streamed state, in its plot's local space, quantized to <see cref="Bits" /> bits.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct AssemblyState(
	int Id,
	Vector3 Position,
	Quaternion Rotation,
	Vector3 LinearVelocity,
	Vector3 AngularVelocity)
{
	public const int Bits = IdBits + 3 * PositionBits + RotationIndexBits + 3 * RotationBits + 6 * VelocityBits;
	public const int MaxId = (1 << IdBits) - 1;

	private const int IdBits = 16;

	// 1/512 m steps (Epsilon) within +-16 km
	private const int PositionBits = 24;
	private const float PositionScale = 512f;

	// Smallest three: the largest component's index, then the others, each within +-1/sqrt(2)
	private const int RotationIndexBits = 2;
	private const int RotationBits = 10;
	private const float RotationRange = (1 << RotationBits) - 1;
	private const float Sqrt2 = 1.41421356f;

	// 1/32 m/s and rad/s steps within +-256
	private const int VelocityBits = 14;
	private const float VelocityScale = 32f;

	public void Write(BitWriter writer)
	{
		writer.Write((ulong)Id, IdBits);
		WriteVector(writer, Position, PositionScale, PositionBits);
		WriteRotation(writer, Rotation);
		WriteVector(writer, LinearVelocity, VelocityScale, VelocityBits);
		WriteVector(writer, AngularVelocity, VelocityScale, VelocityBits);
	}

	public static AssemblyState Read(ref BitReader reader) =>
		new(
			(int)reader.Read(IdBits), ReadVector(ref reader, PositionScale, PositionBits), ReadRotation(ref reader),
			ReadVector(ref reader, VelocityScale, VelocityBits), ReadVector(ref reader, VelocityScale, VelocityBits));

	private static void WriteVector(BitWriter writer, Vector3 vector, float scale, int bits)
	{
		var limit = (1L << (bits - 1)) - 1;

		for (var axis = 0; axis < 3; axis++)
		{
			var steps = (long)MathF.Round(vector[axis] * scale, MidpointRounding.ToEven);
			writer.WriteSigned(Math.Clamp(steps, -limit, limit), bits);
		}
	}

	private static Vector3 ReadVector(ref BitReader reader, float scale, int bits) =>
		new(reader.ReadSigned(bits) / scale, reader.ReadSigned(bits) / scale, reader.ReadSigned(bits) / scale);

	private static void WriteRotation(BitWriter writer, Quaternion rotation)
	{
		var normalized = rotation.Normalized();
		Span<float> components = [normalized.X, normalized.Y, normalized.Z, normalized.W];

		var largest = 0;

		for (var i = 1; i < 4; i++)
			if (MathF.Abs(components[i]) > MathF.Abs(components[largest]))
				largest = i;

		// q and -q are the same rotation, so the largest component is sent as positive
		var sign = components[largest] < 0 ? -1 : 1;
		writer.Write((ulong)largest, RotationIndexBits);

		for (var i = 0; i < 4; i++)
			if (i != largest)
			{
				var unit = Math.Clamp(components[i] * sign * Sqrt2, -1, 1);
				writer.Write((ulong)MathF.Round((unit + 1) / 2 * RotationRange, MidpointRounding.ToEven), RotationBits);
			}
	}

	private static Quaternion ReadRotation(ref BitReader reader)
	{
		var largest = (int)reader.Read(RotationIndexBits);
		Span<float> components = stackalloc float[4];
		var sum = 0f;

		for (var i = 0; i < 4; i++)
		{
			if (i == largest)
				continue;

			components[i] = (reader.Read(RotationBits) / RotationRange * 2 - 1) / Sqrt2;
			sum += components[i] * components[i];
		}

		components[largest] = MathF.Sqrt(MathF.Max(0, 1 - sum));
		return new Quaternion(components[0], components[1], components[2], components[3]).Normalized();
	}
}
