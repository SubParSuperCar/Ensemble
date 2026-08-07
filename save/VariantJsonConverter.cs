using System.Text.Json;
using System.Text.Json.Serialization;
using Root.Core.Api.Asset;

namespace Root.Save;

internal sealed class VariantJsonConverter : JsonConverter<Variant>
{
	public override Variant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
		reader.TokenType switch
		{
			JsonTokenType.Null => Variant.Null,
			JsonTokenType.True => new Variant(true),
			JsonTokenType.False => new Variant(false),
			JsonTokenType.Number when reader.TryGetInt64(out var integer) => new Variant(integer),
			JsonTokenType.Number => new Variant(reader.GetDouble()),
			JsonTokenType.String => new Variant(reader.GetString()),
			_ => throw new JsonException($"Unsupported JSON token: {reader.TokenType}")
		};

	public override void Write(Utf8JsonWriter writer, Variant value, JsonSerializerOptions options)
	{
		switch (value.Type)
		{
			case VariantType.Null:
				writer.WriteNullValue();
				break;

			case VariantType.Bool:
				writer.WriteBooleanValue((bool)value);
				break;

			case VariantType.NumInt:
				writer.WriteNumberValue((long)value);
				break;

			case VariantType.NumDouble:
				writer.WriteNumberValue((double)value);
				break;

			case VariantType.Str:
				writer.WriteStringValue((string)value);
				break;

			default:
				throw new JsonException($"Unsupported variant type: {value.Type}");
		}
	}
}
