namespace Root.Saving;

internal static class SaveCodec
{
	public static void Write(ISaveSerializer serializer, Stream stream, CreationSaveData data, SaveOptions options)
	{
		if (options.Encryption is { } encryption)
		{
			var salt = SaveCrypto.NewSalt();

			var header = SaveEnvelope.WriteHeader(
				stream, options.Compression, EncryptionType.Aes256Gcm, KdfFor(encryption), salt);

			using var plaintext = new MemoryStream();
			Compress(serializer, plaintext, data, options);

			SaveCrypto.Encrypt(
				stream, plaintext.GetBuffer().AsSpan(0, (int)plaintext.Length), header, encryption, salt);
		}
		else
		{
			SaveEnvelope.WriteHeader(stream, options.Compression, EncryptionType.None, KdfParameters.None, default);
			Compress(serializer, stream, data, options);
		}
	}

	public static CreationSaveData Read(ISaveSerializer serializer, Stream stream, LoadOptions options)
	{
		var header = SaveEnvelope.ReadHeader(stream);

		if (header.Encryption is EncryptionType.None)
			return Decompress(serializer, stream, header.Compression);

		var plaintext = SaveCrypto.Decrypt(stream, header, options);
		using var buffer = new MemoryStream(plaintext, writable: false);
		return Decompress(serializer, buffer, header.Compression);
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
