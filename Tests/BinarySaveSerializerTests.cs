using System.Numerics;
using EnsembleCoreRoot.Api.Assets;
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
	public void Deserialize_Empty_RoundTrips()
	{
		var bytes = Serialize(new CreationSaveData());
		var data = Serializer.Deserialize(new MemoryStream(bytes));

		Assert.Empty(data.Instances);
	}

	[Theory]
	[InlineData(InstanceCountOffset, sizeof(int), 0x00)]
	[InlineData(InstanceCountOffset, sizeof(int), 0xFF)]
	[InlineData(UtcTicksOffset, sizeof(long), 0x00)]
	[InlineData(UtcTicksOffset, sizeof(long), 0xFF)]
	[InlineData(PositionOffset, sizeof(float), 0xFF)]
	[InlineData(RotationOffset, 4 * sizeof(float), 0x00)]
	public void Deserialize_FilledField_Throws(int offset, int length, byte fill)
	{
		var bytes = Serialize(CreateSaveData());
		bytes.AsSpan(offset, length).Fill(fill);

		Assert.Throws<InvalidDataException>(() => Serializer.Deserialize(new MemoryStream(bytes)));
	}

	[Fact]
	public void Deserialize_DuplicatePropertyKey_Throws()
	{
		var data = CreateSaveData();
		data.Instances[0] = new SaveInstance
		{
			AssetId = 1,
			Position = Vector3.One,
			Rotation = Quaternion.Identity,
			Properties = new Dictionary<string, CoreVariant>(StringComparer.Ordinal) { ["a"] = 1, ["b"] = 2 }
		};

		var bytes = Serialize(data);
		bytes[Array.LastIndexOf(bytes, (byte)'b')] = (byte)'a';

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
