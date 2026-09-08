namespace Root.Saving;

public enum CompressionType : byte
{
	None,
	ZStandard,
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
		int MemoryKiB = 65_536,
		int Iterations = 3,
		int DegreeOfParallelism = 4) : SaveEncryption;

	public sealed record Key(ReadOnlyMemory<byte> Value) : SaveEncryption;
}

public sealed record SaveOptions
{
	public static SaveOptions Default { get; } = new();

	public CompressionType Compression { get; init; } = CompressionType.None;
	public int? CompressionLevel { get; init; }
	public SaveEncryption? Encryption { get; init; }

	public bool Checksum { get; init; }
}

public sealed record LoadOptions
{
	public static LoadOptions Default { get; } = new();

	public string? Password { get; init; }
	public ReadOnlyMemory<byte>? Key { get; init; }
}
