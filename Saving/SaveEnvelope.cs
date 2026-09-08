using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Root.Saving;

internal enum KdfFunction : byte
{
	None,
	ARGON2_ID
}

[StructLayout(LayoutKind.Auto)]
internal readonly record struct KdfParameters(
	KdfFunction Function,
	int MemoryKiB,
	int Iterations,
	int DegreeOfParallelism)
{
	public static KdfParameters None => new(KdfFunction.None, 0, 0, 0);

	public static KdfParameters Argon2Id(int memoryKiB, int iterations, int degreeOfParallelism) =>
		new(KdfFunction.ARGON2_ID, memoryKiB, iterations, degreeOfParallelism);
}

internal static class SaveEnvelope
{
	public const int NonceSize = 12;
	public const int TagSize = 16;

	private const byte Version = 1;
	private const int PrefixSize = 7;
	private const int KdfBlockSize = 11;

	private static ReadOnlySpan<byte> Magic => "ENSV"u8;

	public static byte[] WriteHeader(
		Stream stream,
		CompressionType compression,
		EncryptionType encryption,
		KdfParameters kdf,
		ReadOnlySpan<byte> salt)
	{
		var encrypted = encryption is not EncryptionType.None;
		var header = new byte[encrypted ? PrefixSize + KdfBlockSize + salt.Length : PrefixSize];

		Magic.CopyTo(header);
		header[4] = Version;
		header[5] = (byte)compression;
		header[6] = (byte)encryption;

		if (encrypted)
		{
			header[7] = (byte)kdf.Function;
			BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(8), (uint)kdf.MemoryKiB);
			BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(12), (uint)kdf.Iterations);
			header[16] = (byte)kdf.DegreeOfParallelism;
			header[17] = checked((byte)salt.Length);
			salt.CopyTo(header.AsSpan(18));
		}

		stream.Write(header);
		return header;
	}

	public static Header ReadHeader(Stream stream)
	{
		Span<byte> prefix = stackalloc byte[PrefixSize];
		stream.ReadExactly(prefix);

		if (!prefix[..4].SequenceEqual(Magic))
			throw new InvalidDataException("Unrecognized save file.");

		if (prefix[4] is not Version)
			throw new InvalidDataException($"Unsupported save envelope version: {prefix[4]}.");

		var compression = (CompressionType)prefix[5];
		var encryption = (EncryptionType)prefix[6];

		if (encryption is EncryptionType.None)
			return new Header(compression, encryption, KdfParameters.None, [], [.. prefix]);

		if (encryption is not EncryptionType.Aes256Gcm)
			throw new InvalidDataException($"Unsupported encryption type: {prefix[6]}.");

		Span<byte> kdf = stackalloc byte[KdfBlockSize];
		stream.ReadExactly(kdf);

		var parameters = new KdfParameters(
			(KdfFunction)kdf[0],
			(int)BinaryPrimitives.ReadUInt32LittleEndian(kdf[1..]),
			(int)BinaryPrimitives.ReadUInt32LittleEndian(kdf[5..]),
			kdf[9]);

		var salt = new byte[kdf[10]];
		stream.ReadExactly(salt);

		var bytes = new byte[PrefixSize + KdfBlockSize + salt.Length];
		prefix.CopyTo(bytes);
		kdf.CopyTo(bytes.AsSpan(PrefixSize));
		salt.CopyTo(bytes.AsSpan(PrefixSize + KdfBlockSize));

		return new Header(compression, encryption, parameters, salt, bytes);
	}

	public readonly record struct Header(
		CompressionType Compression,
		EncryptionType Encryption,
		KdfParameters Kdf,
		byte[] Salt,
		byte[] Bytes);
}
