using System.Security.Cryptography;

namespace Root.Saving.Pipeline;

internal static class SaveCodec
{
	public static void Write(ISaveSerializer serializer, Stream stream, CreationSaveData data, SaveOptions options)
	{
		using var payload = new MemoryStream();
		Compress(serializer, payload, data, options);
		var bytes = payload.GetBuffer().AsSpan(0, (int)payload.Length);

		if (options.Encryption is { } encryption)
		{
			var salt = SaveCrypto.NewSalt();

			var header = SaveEnvelope.WriteHeader(
				stream, options.Compression, EncryptionType.Aes256Gcm,
				SaveFlags.None, default, KdfFor(encryption), salt);

			SaveCrypto.Encrypt(stream, bytes, header, encryption, salt);
			return;
		}

		if (options.Checksum)
		{
			Span<byte> checksum = stackalloc byte[SaveEnvelope.ChecksumSize];
			SHA256.HashData(bytes, checksum);

			SaveEnvelope.WriteHeader(
				stream, options.Compression, EncryptionType.None,
				SaveFlags.Checksum, checksum, KdfParameters.None, default);
		}
		else
		{
			SaveEnvelope.WriteHeader(
				stream, options.Compression, EncryptionType.None,
				SaveFlags.None, default, KdfParameters.None, default);
		}

		stream.Write(bytes);
	}

	public static CreationSaveData Read(ISaveSerializer serializer, Stream stream, LoadOptions options)
	{
		var header = SaveEnvelope.ReadHeader(stream);

		if (header.Encryption is not EncryptionType.None)
		{
			using var decrypted = new MemoryStream(SaveCrypto.Decrypt(stream, header, options), false);
			return Decompress(serializer, decrypted, header.Compression);
		}

		using var payload = new MemoryStream();
		stream.CopyTo(payload);

		if (header.Checksum is { } expected)
		{
			Span<byte> actual = stackalloc byte[SaveEnvelope.ChecksumSize];
			SHA256.HashData(payload.GetBuffer().AsSpan(0, (int)payload.Length), actual);

			if (!CryptographicOperations.FixedTimeEquals(actual, expected))
				throw new InvalidDataException("Save file is corrupt: checksum mismatch.");
		}

		payload.Position = 0;
		return Decompress(serializer, payload, header.Compression);
	}

	private static void Compress(ISaveSerializer serializer, Stream target, CreationSaveData data, SaveOptions options)
	{
		using var compressor =
			SaveCompression.TryCreateCompressor(target, options.Compression, options.CompressionLevel);

		serializer.Serialize(compressor ?? target, data);
	}

	private static CreationSaveData Decompress(ISaveSerializer serializer, Stream source, CompressionType compression)
	{
		using var decompressor = SaveCompression.TryCreateDecompressor(source, compression);
		return serializer.Deserialize(decompressor ?? source);
	}

	private static KdfParameters KdfFor(SaveEncryption encryption) =>
		encryption switch
		{
			SaveEncryption.Password pw =>
				KdfParameters.Argon2Id(pw.MemoryKiB, pw.Iterations, pw.DegreeOfParallelism),
			SaveEncryption.Key => KdfParameters.None,
			_ => throw new ArgumentOutOfRangeException(nameof(encryption))
		};
}
