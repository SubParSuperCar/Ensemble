using System.IO.Compression;
using System.Numerics;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.Saving;
using EnsembleRoot.Saving.SerDes;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class SaveTests : IDisposable
{
	private const string Password = "hunter2";

	// Magic and version, compression, and encryption bytes, then the KDF block's function, memory, iterations, and
	// parallelism
	private const int FlagsOffset = 7;
	private const int SaltLengthOffset = FlagsOffset + 1 + 1 + 2 * sizeof(uint) + 1;

	private readonly string _path = Path.GetTempFileName();

	public void Dispose() => File.Delete(_path);

	[Theory]
	[InlineData(nameof(BinarySaveSerializer), CompressionType.None)]
	[InlineData(nameof(BinarySaveSerializer), CompressionType.Brotli)]
	[InlineData(nameof(JsonSaveSerializer), CompressionType.Zstandard)]
	public void SaveThenLoad_RoundTrips(string format, CompressionType compression)
	{
		var serializer = CreateSerializer(format);
		var original = CreateSaveData();

		serializer.Save(_path, original, new SaveOptions { Compression = compression, UseChecksum = true });

		AssertEquivalent(original, serializer.Load(_path));
	}

	[Fact]
	public void SaveThenLoad_WithPassword_RoundTrips()
	{
		var serializer = new BinarySaveSerializer();
		var original = CreateSaveData();

		serializer.Save(_path, original, new SaveOptions { Encryption = CreatePassword() });

		AssertEquivalent(original, serializer.Load(_path, new LoadOptions { Password = Password }));
	}

	[Fact]
	public void Save_Failing_PreservesExistingFile()
	{
		var serializer = new BinarySaveSerializer();
		var original = CreateSaveData();
		serializer.Save(_path, original);

		Assert.Throws<ArgumentException>(() => serializer.Save(
			_path, CreateSaveData(), new SaveOptions { Encryption = new SaveEncryption.Key(new byte[5]) }));

		AssertEquivalent(original, serializer.Load(_path));
		Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(_path)!, $"{Path.GetFileName(_path)}.*.tmp"));
	}

	[Fact]
	public void SaveThenLoad_WithPasswordInOtherNormalization_RoundTrips()
	{
		var serializer = new BinarySaveSerializer();
		var original = CreateSaveData();

		serializer.Save(_path, original, new SaveOptions { Encryption = CreatePassword("caf\u0065\u0301") });

		AssertEquivalent(original, serializer.Load(_path, new LoadOptions { Password = "caf\u00E9" }));
	}

	[Theory]
	[InlineData(SaltLengthOffset, 15, "salt")]
	[InlineData(FlagsOffset, 0b10, "flags")]
	public void Load_WithInvalidHeaderByte_Throws(int offset, byte value, string reason)
	{
		var serializer = new BinarySaveSerializer();
		serializer.Save(_path, CreateSaveData(), new SaveOptions { Encryption = CreatePassword() });

		var bytes = File.ReadAllBytes(_path);
		bytes[offset] = value;
		File.WriteAllBytes(_path, bytes);

		var exception =
			Assert.Throws<InvalidDataException>(() => serializer.Load(_path, new LoadOptions { Password = Password }));

		Assert.Contains(reason, exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Load_Truncated_ThrowsInvalidData()
	{
		var serializer = new BinarySaveSerializer();
		serializer.Save(_path, CreateSaveData());

		File.WriteAllBytes(_path, File.ReadAllBytes(_path)[..^4]);

		Assert.Throws<InvalidDataException>(() => serializer.Load(_path));
	}

	[Fact]
	public void Load_DecompressionBomb_Throws()
	{
		using (var file = File.Create(_path))
		{
			file.Write([.. "ENSV"u8, 0, (byte)CompressionType.Brotli, 0, 0]);

			using var brotli = new BrotliStream(file, CompressionLevel.Fastest);
			var zeros = new byte[1024 * 1024];

			for (var i = 0; i <= 64; i++)
				brotli.Write(zeros);
		}

		var exception = Assert.Throws<InvalidDataException>(() => new BinarySaveSerializer().Load(_path));
		Assert.Contains("too large", exception.Message, StringComparison.Ordinal);
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

	private static ISaveSerializer CreateSerializer(string format) =>
		format is nameof(JsonSaveSerializer) ? new JsonSaveSerializer() : new BinarySaveSerializer();

	private static SaveEncryption.Password CreatePassword(string password = Password) => new(password, 8 * 1024, 1, 1);

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
