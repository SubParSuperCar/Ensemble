using System.Security.Cryptography;
using ZstdSharp;

namespace EnsembleRoot.Saving.Pipeline;

internal static class SaveCodec
{
	private const int MaxPayloadByteCount = 64 * 1024 * 1024;
	private const int ChunkSize = 64 * 1024;

	public static void Write(ISaveSerializer serializer, Stream stream, CreationSaveData data, SaveOptions options)
	{
		using var payload = new MemoryStream();
		Compress(serializer, payload, data, options);
		var bytes = payload.GetBuffer().AsSpan(0, (int)payload.Length);

		if (options.Encryption is { } encryption)
		{
			var salt = SaveCrypto.CreateSalt();

			var header = SaveEnvelope.WriteHeader(
				stream, options.Compression, EncryptionType.Aes256Gcm, default, GetKdfParameters(encryption), salt);

			SaveCrypto.Encrypt(stream, bytes, header, encryption, salt);
			return;
		}

		var checksum = options.UseChecksum ? stackalloc byte[SaveEnvelope.ChecksumSize] : default;

		if (options.UseChecksum)
			SHA256.HashData(bytes, checksum);

		SaveEnvelope.WriteHeader(
			stream, options.Compression, EncryptionType.None, checksum, KdfParameters.None, default);

		stream.Write(bytes);
	}

	public static CreationSaveData Read(ISaveSerializer serializer, Stream stream, LoadOptions options)
	{
		try
		{
			var header = SaveEnvelope.ReadHeader(stream);

			using var payload = ReadToEnd(stream);
			var bytes = payload.GetBuffer().AsSpan(0, (int)payload.Length);

			if (header.Encryption is not EncryptionType.None)
			{
				using var decrypted = new MemoryStream(SaveCrypto.Decrypt(bytes, header, options), false);
				return Decompress(serializer, decrypted, header.Compression);
			}

			// ReSharper disable once InvertIf
			if (header.Checksum is { } expected)
			{
				Span<byte> actual = stackalloc byte[SaveEnvelope.ChecksumSize];
				SHA256.HashData(bytes, actual);

				if (!CryptographicOperations.FixedTimeEquals(actual, expected))
					throw new InvalidDataException("Save file is corrupt: checksum mismatch.");
			}

			return Decompress(serializer, payload, header.Compression);
		}
		catch (EndOfStreamException exception)
		{
			throw new InvalidDataException("Save file is truncated.", exception);
		}
	}

	private static void Compress(ISaveSerializer serializer, Stream target, CreationSaveData data, SaveOptions options)
	{
		using var compressor =
			SaveCompression.CreateCompressorOrNull(target, options.Compression, options.CompressionLevel);

		serializer.Serialize(compressor ?? target, data);
	}

	private static CreationSaveData Decompress(ISaveSerializer serializer, Stream source, CompressionType compression)
	{
		using var decompressor = SaveCompression.CreateDecompressorOrNull(source, compression);

		if (decompressor is null)
			return serializer.Deserialize(source);

		using var payload = ReadDecompressed(decompressor);
		return serializer.Deserialize(payload);
	}

	// The decoders report corrupt data with their own exception types
	private static MemoryStream ReadDecompressed(Stream decompressor)
	{
		try
		{
			return ReadToEnd(decompressor);
		}
		catch (Exception exception) when (exception is ZstdException or InvalidOperationException)
		{
			throw new InvalidDataException("Save data is corrupt.", exception);
		}
	}

	// Bounds the memory a crafted file or a decompression bomb can take
	private static MemoryStream ReadToEnd(Stream source)
	{
		var payload = new MemoryStream();
		var chunk = new byte[ChunkSize];

		for (int count; (count = source.Read(chunk)) > 0;)
		{
			if (payload.Length + count > MaxPayloadByteCount)
				throw new InvalidDataException("Save data is too large.");

			payload.Write(chunk, 0, count);
		}

		payload.Position = 0;
		return payload;
	}

	private static KdfParameters GetKdfParameters(SaveEncryption encryption) =>
		encryption switch
		{
			SaveEncryption.Password password =>
				KdfParameters.Argon2Id(password.MemoryKiB, password.Iterations, password.DegreeOfParallelism),
			SaveEncryption.Key => KdfParameters.None,
			_ => throw new ArgumentOutOfRangeException(nameof(encryption))
		};
}
