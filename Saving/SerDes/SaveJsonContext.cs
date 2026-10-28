using System.Text.Json.Serialization;

namespace EnsembleRoot.Saving.SerDes;

[JsonSourceGenerationOptions(
	WriteIndented = true,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	Converters = [typeof(CoreVariantJsonConverter), typeof(Vector3JsonConverter), typeof(QuaternionJsonConverter)])]
[JsonSerializable(typeof(CreationSaveData))]
internal sealed partial class SaveJsonContext : JsonSerializerContext;
