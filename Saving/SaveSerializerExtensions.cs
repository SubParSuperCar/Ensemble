using EnsembleRoot.Saving.Pipeline;

namespace EnsembleRoot.Saving;

// TODO: We might want to support progress tracking for the UI and similar consumers
public static class SaveSerializerExtensions
{
	extension(ISaveSerializer serializer)
	{
		/// <remarks>Writes to a temporary file first, so a failed save never clobbers the existing one.</remarks>
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
				// File.Delete throws on a missing directory, which would mask the original exception
				if (File.Exists(tempPath))
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
