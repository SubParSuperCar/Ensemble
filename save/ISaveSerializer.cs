// ReSharper disable UnusedMember.Global

namespace Root.Save;

public interface ISaveSerializer
{
	void Serialize(Stream stream, SaveData data);
	SaveData Deserialize(Stream stream);
}
