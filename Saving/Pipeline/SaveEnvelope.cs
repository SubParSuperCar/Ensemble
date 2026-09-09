using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Root.Saving.Pipeline;

internal enum KdfFunction : byte
{
	None,
	ARGON2_ID
}

[Flags]
internal enum SaveFlags : byte
{
	None = 0,
	Checksum = 1 << 0
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
	public const int ChecksumSize = 32;

	private const byte Version = 0;
	private const int PrefixSize = 8;
	private const int KdfBlockSize = 11;

	private static ReadOnlySpan<byte> Magic => "ENSV"u8;

	public static byte[] WriteHeader(
		Stream stream,
		CompressionType compression,
		EncryptionType encryption,
		SaveFlags flags,
		ReadOnlySpan<byte> checksum,
		KdfParameters kdf,
		ReadOnlySpan<byte> salt)
	{
		var isEncrypted = encryption is not EncryptionType.None;
		var hasChecksum = flags.HasFlag(SaveFlags.Checksum);

		var header = new byte[
			PrefixSize
			+ (hasChecksum ? ChecksumSize : 0)
			+ (isEncrypted ? KdfBlockSize + salt.Length : 0)];

		Magic.CopyTo(header);
		header[4] = Version;
		header[5] = (byte)compression;
		header[6] = (byte)encryption;
		header[7] = (byte)flags;

		var offset = PrefixSize;

		if (hasChecksum)
		{
			checksum.CopyTo(header.AsSpan(offset));
			offset += ChecksumSize;
		}

		if (isEncrypted)
		{
			header[offset] = (byte)kdf.Function;
			BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(offset + 1), (uint)kdf.MemoryKiB);
			BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(offset + 5), (uint)kdf.Iterations);
			header[offset + 9] = (byte)kdf.DegreeOfParallelism;
			header[offset + 10] = checked((byte)salt.Length);
			salt.CopyTo(header.AsSpan(offset + 11));
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
		var flags = (SaveFlags)prefix[7];

		using var authenticated = new MemoryStream();
		authenticated.Write(prefix);

		byte[]? checksum = null;

		if (flags.HasFlag(SaveFlags.Checksum))
		{
			checksum = new byte[ChecksumSize];
			stream.ReadExactly(checksum);
			authenticated.Write(checksum);
		}

		if (encryption is EncryptionType.None)
			return new Header(compression, encryption, flags, checksum, KdfParameters.None, [],
				authenticated.ToArray());

		if (encryption is not EncryptionType.Aes256Gcm)
			throw new InvalidDataException($"Unsupported encryption type: {prefix[6]}.");

		Span<byte> kdfBlock = stackalloc byte[KdfBlockSize];
		stream.ReadExactly(kdfBlock);
		authenticated.Write(kdfBlock);

		var kdfParameters = new KdfParameters(
			(KdfFunction)kdfBlock[0],
			(int)BinaryPrimitives.ReadUInt32LittleEndian(kdfBlock[1..]),
			(int)BinaryPrimitives.ReadUInt32LittleEndian(kdfBlock[5..]),
			kdfBlock[9]);

		var salt = new byte[kdfBlock[10]];
		stream.ReadExactly(salt);
		authenticated.Write(salt);

		return new Header(compression, encryption, flags, checksum, kdfParameters, salt, authenticated.ToArray());
	}

	public readonly record struct Header(
		CompressionType Compression,
		EncryptionType Encryption,
		SaveFlags Flags,
		byte[]? Checksum,
		KdfParameters Kdf,
		byte[] Salt,
		byte[] AssociatedData);
}
