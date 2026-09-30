using System.Text.Json.Serialization;

namespace EnsembleRoot.Saving.SerDes;

[JsonSourceGenerationOptions(
	WriteIndented = true,
	Converters = [typeof(CoreVariantJsonConverter), typeof(Vector3JsonConverter), typeof(QuaternionJsonConverter)])]
[JsonSerializable(typeof(CreationSaveData))]
internal partial class SaveJsonContext : JsonSerializerContext;
