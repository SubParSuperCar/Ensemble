using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnsembleRoot.Saving.SerDes;

internal sealed class Vector3JsonConverter : JsonConverter<Vector3>
{
	public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		Span<float> values = stackalloc float[3];
		FloatArray.Read(ref reader, values);

		return new Vector3(values);
	}

	public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options) =>
		FloatArray.Write(writer, [value.X, value.Y, value.Z]);
}

internal sealed class QuaternionJsonConverter : JsonConverter<Quaternion>
{
	public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		Span<float> values = stackalloc float[4];
		FloatArray.Read(ref reader, values);

		return new Quaternion(values[0], values[1], values[2], values[3]);
	}

	public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options) =>
		FloatArray.Write(writer, [value.X, value.Y, value.Z, value.W]);
}

file static class FloatArray
{
	public static void Read(ref Utf8JsonReader reader, scoped Span<float> values)
	{
		if (reader.TokenType is not JsonTokenType.StartArray)
			throw new JsonException($"Expected an array of {values.Length} numbers.");

		foreach (ref var value in values)
		{
			if (!reader.Read() || reader.TokenType is not JsonTokenType.Number)
				throw new JsonException($"Expected an array of {values.Length} numbers.");

			value = reader.GetSingle();
		}

		if (!reader.Read() || reader.TokenType is not JsonTokenType.EndArray)
			throw new JsonException($"Expected an array of {values.Length} numbers.");
	}

	public static void Write(Utf8JsonWriter writer, ReadOnlySpan<float> values)
	{
		writer.WriteStartArray();

		foreach (var value in values)
			writer.WriteNumberValue(value);

		writer.WriteEndArray();
	}
}
