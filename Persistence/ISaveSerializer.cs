namespace Root.Persistence;

public interface ISaveSerializer
{
	void Serialize(Stream stream, CreationSaveData data);
	CreationSaveData Deserialize(Stream stream);
}
