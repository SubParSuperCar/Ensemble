using EnsembleRoot.Saving.Pipeline;

namespace EnsembleRoot.Saving;

public static class SaveSerializerExtensions
{
	extension(ISaveSerializer serializer)
	{
		public void Save(string path, CreationSaveData data, SaveOptions? options = null)
		{
			var tempPath = $"{path}.tmp";

			try
			{
				using (var file = File.Create(tempPath))
				{
					SaveCodec.Write(serializer, file, data, options ?? SaveOptions.Default);
					file.Flush(true);
				}

				File.Move(tempPath, path, true);
			}
			catch
			{
				File.Delete(tempPath);
				throw;
			}
		}

		public CreationSaveData Load(string path, LoadOptions? options = null)
		{
			using var file = File.OpenRead(path);
			return SaveCodec.Read(serializer, file, options ?? LoadOptions.Default);
		}
	}
}
