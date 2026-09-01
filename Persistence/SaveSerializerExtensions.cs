using ZstdSharp;

namespace Root.Persistence;

public enum CompressionType : byte
{
	None,
	Zstd
}

public static class SaveSerializerExtensions
{
	extension(ISaveSerializer serializer)
	{
		public void Save(
			string path,
			CreationSaveData data,
			CompressionType compressionType = CompressionType.None,
			int? compressionLevel = null)
		{
			using var file = File.Create(path);

			switch (compressionType)
			{
				case CompressionType.None:
					serializer.Serialize(file, data);
					break;

				case CompressionType.Zstd:
					{
						using var stream = compressionLevel is { } level
							? new CompressionStream(file, level)
							: new CompressionStream(file);

						serializer.Serialize(stream, data);
						break;
					}

				default:
					throw new ArgumentOutOfRangeException(nameof(compressionType));
			}
		}

		public CreationSaveData Load(string path, CompressionType compressionType = CompressionType.None)
		{
			using var file = File.OpenRead(path);

			return compressionType switch
			{
				CompressionType.None => serializer.Deserialize(file),
				CompressionType.Zstd => DeserializeCompressed(file),
				_ => throw new ArgumentOutOfRangeException(nameof(compressionType))
			};

			CreationSaveData DeserializeCompressed(Stream stream)
			{
				using var decompressor = new DecompressionStream(stream);
				return serializer.Deserialize(decompressor);
			}
		}
	}
}
