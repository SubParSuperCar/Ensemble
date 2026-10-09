// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace EnsembleRoot.Saving;

public enum CompressionType : byte
{
	None,
	Zstandard,
	Brotli
}

public enum EncryptionType : byte
{
	None,
	Aes256Gcm
}

public abstract record SaveEncryption
{
	private SaveEncryption() { }

	public sealed record Password(
		string Secret,
		int MemoryKiB = 64 * 1024,
		int Iterations = 3,
		int DegreeOfParallelism = 4) : SaveEncryption;

	public sealed record Key(ReadOnlyMemory<byte> Value) : SaveEncryption;
}

public sealed record SaveOptions
{
	public static SaveOptions Default { get; } = new();

	public CompressionType Compression { get; init; } = CompressionType.None;

	/// <remarks>
	///     Zstandard takes its native level. Brotli maps 0 to none, 1 to fastest, 2 to optimal (the default), and 3 or
	///     more to smallest.
	/// </remarks>
	public int? CompressionLevel { get; init; }

	public SaveEncryption? Encryption { get; init; }

	/// <remarks>Ignored when encrypted, since AES-GCM already authenticates the payload.</remarks>
	public bool UseChecksum { get; init; }
}

public sealed record LoadOptions
{
	public static LoadOptions Default { get; } = new();

	public string? Password { get; init; }
	public ReadOnlyMemory<byte>? Key { get; init; }
}
