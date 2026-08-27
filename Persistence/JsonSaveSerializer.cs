using System.Text.Json;

namespace Root.Persistence;

// ReSharper disable once UnusedType.Global
public sealed class JsonSaveSerializer : ISaveSerializer
{
	private static readonly JsonSerializerOptions Options = new()
	{
		WriteIndented = true,
		Converters = { new CoreVariantJsonConverter() }
	};

	public void Serialize(Stream stream, CreationSaveData data) => JsonSerializer.Serialize(stream, data, Options);

	public CreationSaveData Deserialize(Stream stream) =>
		JsonSerializer.Deserialize<CreationSaveData>(stream, Options)
		?? throw new InvalidDataException("Failed to deserialize save data.");
}
