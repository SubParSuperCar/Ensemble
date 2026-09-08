namespace Root.Saving;

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
