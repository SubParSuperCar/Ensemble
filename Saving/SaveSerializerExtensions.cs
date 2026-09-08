namespace Root.Saving;

public static class SaveSerializerExtensions
{
	extension(ISaveSerializer serializer)
	{
		public void Save(string path, CreationSaveData data, SaveOptions? options = null)
		{
			using var file = File.Create(path);
			SaveCodec.Write(serializer, file, data, options ?? SaveOptions.Default);
		}

		public CreationSaveData Load(string path, LoadOptions? options = null)
		{
			using var file = File.OpenRead(path);
			return SaveCodec.Read(serializer, file, options ?? LoadOptions.Default);
		}
	}
}
