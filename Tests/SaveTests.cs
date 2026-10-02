using System.Numerics;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.Saving;
using EnsembleRoot.Saving.SerDes;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class SaveTests : IDisposable
{
	private const string Password = "hunter2";

	private readonly string _path = Path.GetTempFileName();

	public static TheoryData<string, CompressionType> Formats =>
		new MatrixTheoryData<string, CompressionType>(
			[nameof(BinarySaveSerializer), nameof(JsonSaveSerializer)],
			Enum.GetValues<CompressionType>());

	public void Dispose() => File.Delete(_path);

	[Theory]
	[MemberData(nameof(Formats))]
	public void SaveThenLoad_RoundTrips(string format, CompressionType compression)
	{
		var serializer = CreateSerializer(format);
		var original = CreateSaveData();

		serializer.Save(_path, original, new SaveOptions { Compression = compression, UseChecksum = true });
		AssertEquivalent(original, serializer.Load(_path));
	}

	[Theory]
	[MemberData(nameof(Formats))]
	public void SaveThenLoad_WithPassword_RoundTrips(string format, CompressionType compression)
	{
		var serializer = CreateSerializer(format);
		var original = CreateSaveData();

		serializer.Save(_path, original, new SaveOptions { Compression = compression, Encryption = CreatePassword() });
		AssertEquivalent(original, serializer.Load(_path, new LoadOptions { Password = Password }));
	}

	[Fact]
	public void Load_WithWrongPassword_Throws()
	{
		var serializer = new BinarySaveSerializer();
		serializer.Save(_path, CreateSaveData(), new SaveOptions { Encryption = CreatePassword() });

		Assert.Throws<InvalidDataException>(() => serializer.Load(_path, new LoadOptions { Password = "hunter3" }));
	}

	[Fact]
	public void Load_WithCorruptPayload_Throws()
	{
		var serializer = new BinarySaveSerializer();
		serializer.Save(_path, CreateSaveData(), new SaveOptions { UseChecksum = true });

		var bytes = File.ReadAllBytes(_path);
		bytes[^1] ^= 0xFF;
		File.WriteAllBytes(_path, bytes);

		Assert.Throws<InvalidDataException>(() => serializer.Load(_path));
	}

	[Fact]
	public void Load_WithUnknownCompression_Throws()
	{
		var serializer = new BinarySaveSerializer();
		serializer.Save(_path, CreateSaveData());

		var bytes = File.ReadAllBytes(_path);
		bytes[5] = 0xFF;
		File.WriteAllBytes(_path, bytes);

		Assert.Throws<InvalidDataException>(() => serializer.Load(_path));
	}

	private static ISaveSerializer CreateSerializer(string format) =>
		format is nameof(JsonSaveSerializer) ? new JsonSaveSerializer() : new BinarySaveSerializer();

	private static SaveEncryption.Password CreatePassword() => new(Password, 8 * 1024, 1, 1);

	private static CreationSaveData CreateSaveData() =>
		new()
		{
			UtcCreatedAt = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero),
			Instances =
			{
				new SaveInstance
				{
					AssetId = 1,
					Position = new Vector3(1.5f, -2, 3.25f),
					Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2)
				},
				new SaveInstance
				{
					AssetId = 2,
					Position = new Vector3(0, 4, -8),
					Rotation = Quaternion.Identity,
					Properties = new Dictionary<string, CoreVariant>(StringComparer.Ordinal)
					{
						["_colorHex"] = "FF8000",
						["_materialId"] = 3,
						["_opacity"] = 0.5,
						["_isAnchored"] = true
					}
				}
			}
		};

	private static void AssertEquivalent(CreationSaveData expected, CreationSaveData actual)
	{
		Assert.Equal(expected.Version, actual.Version);
		Assert.Equal(expected.UtcCreatedAt, actual.UtcCreatedAt);
		Assert.Equal(expected.Instances.Count, actual.Instances.Count);

		foreach (var (left, right) in expected.Instances.Zip(actual.Instances))
		{
			Assert.Equal(left.AssetId, right.AssetId);
			Assert.Equal(left.Position, right.Position);
			Assert.Equal(left.Rotation, right.Rotation);
			Assert.Equal(left.Properties ?? [], right.Properties ?? []);
		}
	}
}
