using System.Buffers.Binary;
using System.Numerics;
using EnsembleRoot.Saving;
using EnsembleRoot.Saving.SerDes;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class BinarySaveSerializerTests
{
	private const int UtcTicksOffset = 6;
	private const int InstanceCountOffset = UtcTicksOffset + sizeof(long);
	private const int PositionOffset = InstanceCountOffset + sizeof(int) + sizeof(ushort);
	private const int RotationOffset = PositionOffset + 3 * sizeof(float);

	private static readonly BinarySaveSerializer Serializer = new();

	[Fact]
	public void Deserialize_Empty_RoundTrips() =>
		Assert.Empty(Serializer.Deserialize(new MemoryStream(Serialize(new CreationSaveData()))).Instances);

	[Theory]
	[InlineData(0x00)]
	[InlineData(0xFF)]
	public void Deserialize_FilledInstanceCount_Throws(byte fill) =>
		AssertCorrupt(bytes => bytes.AsSpan(InstanceCountOffset, sizeof(int)).Fill(fill));

	[Theory]
	[InlineData(0x00)]
	[InlineData(0xFF)]
	public void Deserialize_FilledCreationTime_Throws(byte fill) =>
		AssertCorrupt(bytes => bytes.AsSpan(UtcTicksOffset, sizeof(long)).Fill(fill));

	[Fact]
	public void Deserialize_NaNPosition_Throws() =>
		AssertCorrupt(static bytes => bytes.AsSpan(PositionOffset, sizeof(float)).Fill(0xFF));

	[Fact]
	public void Deserialize_ZeroRotation_Throws() =>
		AssertCorrupt(static bytes => bytes.AsSpan(RotationOffset, 4 * sizeof(float)).Clear());

	[Fact]
	public void Serialize_StoresInstanceCountPlusOne() =>
		Assert.Equal(2,
			BinaryPrimitives.ReadInt32LittleEndian(Serialize(CreateSaveData()).AsSpan(InstanceCountOffset)));

	private static void AssertCorrupt(Action<byte[]> corrupt)
	{
		var bytes = Serialize(CreateSaveData());
		corrupt(bytes);

		Assert.Throws<InvalidDataException>(() => Serializer.Deserialize(new MemoryStream(bytes)));
	}

	private static byte[] Serialize(CreationSaveData data)
	{
		using var stream = new MemoryStream();
		Serializer.Serialize(stream, data);

		return stream.ToArray();
	}

	private static CreationSaveData CreateSaveData() =>
		new()
		{
			UtcCreatedAt = new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero),
			Instances = { new SaveInstance { AssetId = 1, Position = Vector3.One, Rotation = Quaternion.Identity } }
		};
}
