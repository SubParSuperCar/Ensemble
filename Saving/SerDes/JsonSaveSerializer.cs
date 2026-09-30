using System.Text.Json;

namespace EnsembleRoot.Saving.SerDes;

public sealed class JsonSaveSerializer : ISaveSerializer
{
	public void Serialize(Stream stream, CreationSaveData data) =>
		JsonSerializer.Serialize(stream, data, SaveJsonContext.Default.CreationSaveData);

	public CreationSaveData Deserialize(Stream stream) =>
		JsonSerializer.Deserialize(stream, SaveJsonContext.Default.CreationSaveData)
		?? throw new InvalidDataException("Failed to deserialize save data.");
}
