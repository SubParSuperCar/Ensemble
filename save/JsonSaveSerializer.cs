using System.Text.Json;

namespace Root.Save;

// ReSharper disable once UnusedType.Global
public sealed class JsonSaveSerializer : ISaveSerializer
{
	private static readonly JsonSerializerOptions Options = new()
	{
		WriteIndented = true
	};

	public void Serialize(Stream stream, SaveData data) => JsonSerializer.Serialize(stream, data, Options);

	public SaveData Deserialize(Stream stream) =>
		JsonSerializer.Deserialize<SaveData>(stream, Options)
		?? throw new InvalidDataException("Failed to deserialize save data.");
}
