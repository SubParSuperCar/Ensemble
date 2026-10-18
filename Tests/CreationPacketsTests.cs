using EnsembleRoot.Common.Networking;
using EnsembleRoot.Scripts.Plots.Impl;
using Godot;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class CreationPacketsTests
{
	[Fact]
	public void BitWriterAndReader_RoundTripAnyWidthAndSign_ThenFlagOverruns()
	{
		var random = new Random(1);
		var writer = new BitWriter(1);
		var values = new List<(ulong Value, long Signed, int Bits)>();

		for (var i = 0; i < 500; i++)
		{
			var bits = random.Next(1, 65);
			var value = ((ulong)random.NextInt64() << 1) ^ (ulong)random.Next(2);
			var mask = bits is 64 ? ulong.MaxValue : (1UL << bits) - 1;
			var signed = bits is 64 ? (long)value : (long)(value & mask) - (1L << (bits - 1));

			values.Add((value & mask, signed, bits));
			writer.Write(value, bits);
			writer.WriteSigned(signed, bits);
		}

		var reader = new BitReader(writer.ToArray());

		foreach (var (value, signed, bits) in values)
		{
			Assert.Equal(value, reader.Read(bits));
			Assert.Equal(signed, reader.ReadSigned(bits));
		}

		Assert.False(reader.IsOverrun);
		Assert.Equal(0UL, reader.Read(64));
		Assert.True(reader.IsOverrun);
	}

	[Fact]
	public void AssemblyState_RoundTripsWithinItsQuantization_AndClampsOutOfRange()
	{
		var random = new Random(2);
		var writer = new BitWriter();
		var states = new List<AssemblyState>();

		for (var i = 0; i < 500; i++)
		{
			var rotation =
				new Quaternion(Next(random, 1), Next(random, 1), Next(random, 1), Next(random, 1)).Normalized();

			states.Add(new AssemblyState(
				random.Next(0, 1 << 16), new Vector3(Next(random, 16000), Next(random, 16000), Next(random, 16000)),
				i % 2 is 0 ? rotation : -rotation, new Vector3(Next(random, 255), Next(random, 255), Next(random, 255)),
				new Vector3(Next(random, 255), Next(random, 255), Next(random, 255))));
		}

		foreach (var state in states)
			state.Write(writer);

		var reader = new BitReader(writer.ToArray());

		foreach (var state in states)
		{
			var read = AssemblyState.Read(ref reader);

			Assert.Equal(state.Id, read.Id);
			Assert.True(IsWithin(read.Position, state.Position, 1 / 1024f));
			Assert.True(read.Rotation.AngleTo(state.Rotation) < 1 / 128f);
			Assert.True(IsWithin(read.LinearVelocity, state.LinearVelocity, 1 / 64f));
			Assert.True(IsWithin(read.AngularVelocity, state.AngularVelocity, 1 / 64f));
		}

		var far = new AssemblyState(0, Vector3.One * 1e9f, Quaternion.Identity, Vector3.One * -1e9f, Vector3.Zero);
		writer.Clear();
		far.Write(writer);

		reader = new BitReader(writer.ToArray());
		var clamped = AssemblyState.Read(ref reader);

		Assert.True(clamped.Position.X is > 16383 and < 16384);
		Assert.True(clamped.LinearVelocity.X is < -255 and > -256);
	}

	[Fact]
	public void Packets_RoundTrip_AndRejectTruncatedOrInflatedData()
	{
		var header = new CreationPacketHeader(7, 65535, 12345);
		AssemblyState[] states =
		[
			new(1, Vector3.One, Quaternion.Identity, Vector3.Zero, Vector3.Up),
			new(42, -Vector3.One, Quaternion.Identity, Vector3.Right, Vector3.Zero)
		];

		var data = CreationPackets.Write(new BitWriter(), header, states);
		var read = new List<AssemblyState>();

		Assert.True(CreationPackets.TryRead(data, out var readHeader, read));
		Assert.Equal(header, readHeader);
		Assert.Equal([1, 42], read.Select(static state => state.Id));

		Assert.False(CreationPackets.TryRead(data.AsSpan(0, data.Length - 4), out _, read));

		// A count claiming more states than the data holds is rejected before decoding any
		var inflated = (byte[])data.Clone();
		inflated[5] = 0xFF;
		inflated[6] = 0xFF;

		Assert.False(CreationPackets.TryRead(inflated, out _, read));
		Assert.Empty(read);
	}

	private static float Next(Random random, float range) => (random.NextSingle() * 2 - 1) * range;

	private static bool IsWithin(Vector3 actual, Vector3 expected, float tolerance) =>
		(actual - expected).Abs() is var error && error.X <= tolerance && error.Y <= tolerance && error.Z <= tolerance;
}
