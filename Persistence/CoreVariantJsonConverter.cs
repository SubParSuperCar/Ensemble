using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreRoot.Api.Assets;

namespace Root.Persistence;

internal sealed class CoreVariantJsonConverter : JsonConverter<CoreVariant>
{
	public override CoreVariant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
		reader.TokenType switch
		{
			JsonTokenType.Null => CoreVariant.Null,
			JsonTokenType.True => new CoreVariant(true),
			JsonTokenType.False => new CoreVariant(false),
			JsonTokenType.Number when reader.TryGetInt64(out var integer) => new CoreVariant(integer),
			JsonTokenType.Number => new CoreVariant(reader.GetDouble()),
			JsonTokenType.String => new CoreVariant(reader.GetString()),
			_ => throw new JsonException($"Unsupported JSON token: {reader.TokenType}")
		};

	public override void Write(Utf8JsonWriter writer, CoreVariant value, JsonSerializerOptions options)
	{
		switch (value.Type)
		{
			case CoreVariantType.Null:
				writer.WriteNullValue();
				break;

			case CoreVariantType.Bool:
				writer.WriteBooleanValue((bool)value);
				break;

			case CoreVariantType.NumInt:
				writer.WriteNumberValue((long)value);
				break;

			case CoreVariantType.NumDouble:
				WriteDouble(writer, (double)value);
				break;

			case CoreVariantType.Str:
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
