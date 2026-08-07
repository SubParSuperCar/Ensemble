// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Root.Save;

public static class SaveSerializerExtensions
{
	extension(ISaveSerializer serializer)
	{
		public byte[] Serialize(SaveData data)
		{
			using var stream = new MemoryStream();
			serializer.Serialize(stream, data);
			return stream.ToArray();
		}

		public SaveData Deserialize(byte[] data)
		{
			using var stream = new MemoryStream(data);
			return serializer.Deserialize(stream);
		}
	}
}
