using System.Text.Json;

namespace Root.Persistence;

public sealed class JsonSaveSerializer : ISaveSerializer
{
	public void Serialize(Stream stream, CreationSaveData data) =>
		JsonSerializer.Serialize(stream, data, SaveJsonContext.Instance.CreationSaveData);

	public CreationSaveData Deserialize(Stream stream) =>
		JsonSerializer.Deserialize(stream, SaveJsonContext.Instance.CreationSaveData)
		?? throw new InvalidDataException("Failed to deserialize save data.");
}
