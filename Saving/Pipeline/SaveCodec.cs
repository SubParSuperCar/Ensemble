using System.Security.Cryptography;
using EnsembleRoot.Saving.SerDes;
using ZstdSharp;

namespace EnsembleRoot.Saving.Pipeline;

internal static class SaveCodec
{
	private const int MaxPayloadByteCount = 64 * 1024 * 1024;
	private const int ChunkSize = 64 * 1024;

	public static void Write(ISaveSerializer serializer, Stream stream, CreationSaveData data, SaveOptions options)
	{
		// The envelope only carries compression, encryption, and checksums, so a plain save is its bare payload, and a
		// plain JSON save is a JSON file
		if (options is { Compression: CompressionType.None, Encryption: null, UseChecksum: false })
		{
			serializer.Serialize(stream, data);
			return;
		}

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

	public static CreationSaveData Read(ISaveSerializer serializer, Stream stream, LoadOptions options) =>
		ReportTruncation(() =>
		{
			using var payload = ReadPayload(stream, options);
			return serializer.Deserialize(payload);
		});

	/// <summary>Reads a save of either format, told apart by how its payload starts.</summary>
	public static (SaveFormat Format, CreationSaveData Data) Read(Stream stream, LoadOptions options) =>
		ReportTruncation(() =>
		{
			using var payload = ReadPayload(stream, options);

			Span<byte> start = stackalloc byte[BinarySaveSerializer.MagicSize];
			var count = payload.ReadAtLeast(start, start.Length, false);
			payload.Position = 0;

			var format = BinarySaveSerializer.StartsWithMagic(start[..count]) ? SaveFormat.Binary : SaveFormat.Json;
			return (format, SaveFile.GetSerializer(format).Deserialize(payload));
		});

	private static T ReportTruncation<T>(Func<T> read)
	{
		try
		{
			return read();
		}
		catch (EndOfStreamException exception)
		{
			throw new InvalidDataException("Save file is truncated.", exception);
		}
	}

	private static Stream ReadPayload(Stream stream, LoadOptions options)
	{
		var hasEnvelope = SaveEnvelope.TryReadHeader(stream, out var header);
		var payload = ReadToEnd(stream);

		if (!hasEnvelope)
			return payload;

		var bytes = payload.GetBuffer().AsSpan(0, (int)payload.Length);

		if (header.Encryption is not EncryptionType.None)
		{
			var decrypted = new MemoryStream(SaveCrypto.Decrypt(bytes, header, options), false);
			payload.Dispose();

			return Decompress(decrypted, header.Compression);
		}

		// ReSharper disable once InvertIf
		if (header.Checksum is { } expected)
		{
			Span<byte> actual = stackalloc byte[SaveEnvelope.ChecksumSize];
			SHA256.HashData(bytes, actual);

			if (!CryptographicOperations.FixedTimeEquals(actual, expected))
				throw new InvalidDataException("Save file is corrupt: checksum mismatch.");
		}

		return Decompress(payload, header.Compression);
	}

	private static void Compress(ISaveSerializer serializer, Stream target, CreationSaveData data, SaveOptions options)
	{
		using var compressor =
			SaveCompression.CreateCompressorOrNull(target, options.Compression, options.CompressionLevel);

		serializer.Serialize(compressor ?? target, data);
	}

	private static Stream Decompress(Stream source, CompressionType compression)
	{
		if (SaveCompression.CreateDecompressorOrNull(source, compression) is not { } decompressor)
			return source;

		using (source)
		using (decompressor)
			return ReadDecompressed(decompressor);
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
