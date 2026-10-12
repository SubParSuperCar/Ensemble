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

	[Fact]
	public void Deserialize_UnnormalizedRotation_IsNormalized()
	{
		var data = CreateSaveData();
		data.Instances[0] = new SaveInstance { AssetId = 1, Rotation = new Quaternion(0f, 0f, 0f, 2f) };

		var instance = Serializer.Deserialize(new MemoryStream(Serialize(data))).Instances[0];

		Assert.Equal(Quaternion.Identity, instance.Rotation);
	}

	[Fact]
	public void Serialize_OversizedKey_Throws()
	{
		var data = CreateSaveData();
		data.Instances[0] = new SaveInstance
		{
			AssetId = 1,
			Rotation = Quaternion.Identity,
			Properties = new Dictionary<string, CoreVariant>(StringComparer.Ordinal) { [new string('k', 257)] = 1 }
		};

		Assert.Throws<InvalidOperationException>(() => Serialize(data));
	}

	[Fact]
	public void Deserialize_OversizedStringLength_Throws()
	{
		var data = CreateSaveData();
		data.Instances[0] = new SaveInstance
		{
			AssetId = 1,
			Rotation = Quaternion.Identity,
			Properties = new Dictionary<string, CoreVariant>(StringComparer.Ordinal) { ["k"] = "v" }
		};

		var bytes = Serialize(data);

		// Swaps the value's one-byte length prefix for a 7-bit-encoded length of 2 MiB - 1
		byte[] corrupted = [.. bytes[..^2], 0xFF, 0xFF, 0x7F, .. bytes[^1..]];

		Assert.Throws<InvalidDataException>(() => Serializer.Deserialize(new MemoryStream(corrupted)));
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
