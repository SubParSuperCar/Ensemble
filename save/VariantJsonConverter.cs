using System.Globalization;
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
				WriteDouble(writer, (double)value);
				break;

			case VariantType.Str:
				writer.WriteStringValue((string)value);
				break;

			default:
				throw new JsonException($"Unsupported variant type: {value.Type}");
		}
	}

	private static void WriteDouble(Utf8JsonWriter writer, double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
			throw new JsonException($"Cannot represent {value.ToString(CultureInfo.InvariantCulture)} in JSON.");

		var text = value.ToString(CultureInfo.InvariantCulture);

		if (!text.Contains('.', StringComparison.Ordinal) && !text.Contains('E', StringComparison.Ordinal))
			text = string.Concat(text, ".0");

		writer.WriteRawValue(text);
	}
}
