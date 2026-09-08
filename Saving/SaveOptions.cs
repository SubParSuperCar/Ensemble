namespace Root.Saving;

public sealed record SaveOptions
{
	public static SaveOptions Default { get; } = new();

	public CompressionType Compression { get; init; } = CompressionType.None;
	public int? CompressionLevel { get; init; }
	public SaveEncryption? Encryption { get; init; }
}

public sealed record LoadOptions
{
	public static LoadOptions Default { get; } = new();

	public string? Password { get; init; }
	public ReadOnlyMemory<byte>? Key { get; init; }
}
