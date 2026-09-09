using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Root.Saving.Pipeline;

internal static class SaveCrypto
{
	private const int KeySize = 32;
	private const int SaltSize = 16;

	public static byte[] CreateSalt()
	{
		var salt = new byte[SaltSize];
		RandomNumberGenerator.Fill(salt);
		return salt;
	}

	public static void Encrypt(
		Stream stream,
		ReadOnlySpan<byte> plaintext,
		ReadOnlySpan<byte> associatedData,
		SaveEncryption encryption,
		ReadOnlySpan<byte> salt)
	{
		var key = DeriveKey(encryption, salt);

		try
		{
			Span<byte> nonce = stackalloc byte[SaveEnvelope.NonceSize];
			RandomNumberGenerator.Fill(nonce);

			Span<byte> tag = stackalloc byte[SaveEnvelope.TagSize];
			var ciphertext = new byte[plaintext.Length];

			using var aes = new AesGcm(key, SaveEnvelope.TagSize);
			aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

			stream.Write(nonce);
			stream.Write(tag);
			stream.Write(ciphertext);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(key);
		}
	}

	public static byte[] Decrypt(Stream stream, SaveEnvelope.Header header, LoadOptions options)
	{
		Span<byte> nonce = stackalloc byte[SaveEnvelope.NonceSize];
		stream.ReadExactly(nonce);

		Span<byte> tag = stackalloc byte[SaveEnvelope.TagSize];
		stream.ReadExactly(tag);

		using var buffer = new MemoryStream();
		stream.CopyTo(buffer);
		var ciphertext = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);

		var key = DeriveKey(header, options);

		try
		{
			var plaintext = new byte[ciphertext.Length];
			using var aes = new AesGcm(key, SaveEnvelope.TagSize);

			try
			{
				aes.Decrypt(nonce, ciphertext, tag, plaintext, header.AssociatedData);
			}
			catch (AuthenticationTagMismatchException exception)
			{
				throw new InvalidDataException("Wrong key, or the save file has been tampered with.", exception);
			}

			return plaintext;
		}
		finally
		{
			CryptographicOperations.ZeroMemory(key);
		}
	}

	private static byte[] DeriveKey(SaveEncryption encryption, ReadOnlySpan<byte> salt) =>
		encryption switch
		{
			SaveEncryption.Key key => ValidateKey(key.Value.Span),
			SaveEncryption.Password password => DeriveArgon2IdKey(
				Encoding.UTF8.GetBytes(password.Secret), salt,
				password.MemoryKiB, password.Iterations, password.DegreeOfParallelism),
			_ => throw new ArgumentOutOfRangeException(nameof(encryption))
		};

	private static byte[] DeriveKey(SaveEnvelope.Header header, LoadOptions options) =>
		header.Kdf.Function switch
		{
			KdfFunction.None => ValidateKey((options.Key ?? throw Missing("raw key")).Span),
			KdfFunction.ARGON2_ID => DeriveArgon2IdKey(
				Encoding.UTF8.GetBytes(options.Password ?? throw Missing("password")),
				header.Salt,
				header.Kdf.MemoryKiB,
				header.Kdf.Iterations,
				header.Kdf.DegreeOfParallelism),
			_ => throw new InvalidDataException($"Unknown key-derivation function: {(byte)header.Kdf.Function}.")
		};

	private static byte[] DeriveArgon2IdKey(
		byte[] password, ReadOnlySpan<byte> salt, int memoryKiB, int iterations, int degreeOfParallelism)
	{
		try
		{
			using var argon2 = new Argon2id(password);
			argon2.Salt = [.. salt];
			argon2.MemorySize = memoryKiB;
			argon2.Iterations = iterations;
			argon2.DegreeOfParallelism = degreeOfParallelism;

			return argon2.GetBytes(KeySize);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(password);
		}
	}

	private static byte[] ValidateKey(ReadOnlySpan<byte> key) =>
		key.Length is KeySize
			? key.ToArray()
			: throw new ArgumentException($"Encryption key must be exactly {KeySize} bytes.", nameof(key));

	private static InvalidOperationException Missing(string material) =>
		new($"This save file is encrypted; a {material} is required to load it.");
}
