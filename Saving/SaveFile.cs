using EnsembleRoot.Saving.Pipeline;
using EnsembleRoot.Saving.SerDes;

namespace EnsembleRoot.Saving;

public enum SaveFormat : byte
{
	Binary,
	Json
}

/// <summary>What a save tells without its password; an encrypted one hides its format and content.</summary>
public sealed record SaveSummary(
	SaveFormat? Format,
	CompressionType Compression,
	bool IsEncrypted,
	bool HasChecksum,
	int? InstanceCount,
	DateTimeOffset? UtcCreatedAt);

/// <summary>Save files of either format, read without knowing which, as names can mislead.</summary>
public static class SaveFile
{
	public static ISaveSerializer GetSerializer(SaveFormat format) =>
		format switch
		{
			SaveFormat.Binary => new BinarySaveSerializer(),
			SaveFormat.Json => new JsonSaveSerializer(),
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
		};

	public static CreationSaveData Load(string path, LoadOptions? options = null)
	{
		using var file = File.OpenRead(path);
		return SaveCodec.Read(file, options ?? LoadOptions.Default).Data;
	}

	/// <summary>Reads a whole save unless it's encrypted, in which case only its envelope.</summary>
	public static SaveSummary Summarize(string path)
	{
		using var file = File.OpenRead(path);

		var hasEnvelope = SaveEnvelope.TryReadHeader(file, out var header);
		var compression = hasEnvelope ? header.Compression : CompressionType.None;

		if (header.Encryption is not EncryptionType.None)
			return new SaveSummary(null, compression, true, false, null, null);

		file.Position = 0;
		var (format, data) = SaveCodec.Read(file, LoadOptions.Default);

		return new SaveSummary(
			format, compression, false, header.Checksum is not null, data.Instances.Count, data.UtcCreatedAt);
	}
}
