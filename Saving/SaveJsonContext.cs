using System.Text.Json;
using System.Text.Json.Serialization;

namespace Root.Saving;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CreationSaveData))]
internal partial class SaveJsonContext : JsonSerializerContext
{
	public static readonly SaveJsonContext Instance = new(
		new JsonSerializerOptions
		{
			WriteIndented = true,
			Converters = { new CoreVariantJsonConverter() }
		});
}
