using System.Globalization;
using System.Text;
using EnsembleCoreRoot.Api.Assets;

namespace EnsembleRoot.Saving.SerDes;

internal static class CoreVariantSerializer
{
	public static CoreVariant Read(BinaryReader reader)
	{
		var type = (CoreVariantType)reader.ReadByte();

		return type switch
		{
			CoreVariantType.Null => CoreVariant.Null,
			CoreVariantType.Boolean => new CoreVariant(reader.ReadBoolean()),
			CoreVariantType.Int64 => new CoreVariant(reader.ReadInt64()),
			CoreVariantType.Double => reader.ReadDouble() is var value && double.IsFinite(value)
				? new CoreVariant(value)
				: throw new InvalidDataException("Invalid floating-point value."),
			CoreVariantType.String => new CoreVariant(ReadString(reader, SaveValidation.MaxStringByteCount)),
			_ => throw new InvalidDataException($"Unsupported variant type: {type}.")
		};
	}

	/// <summary>Reads a string written by <see cref="WriteString" />, up to a byte count.</summary>
	public static string ReadString(BinaryReader reader, int maxByteCount)
	{
		var byteCount = reader.Read7BitEncodedInt();
		if (byteCount < 0 || byteCount > maxByteCount)
			throw new InvalidDataException("Invalid string length.");

		var bytes = reader.ReadBytes(byteCount);
		return bytes.Length == byteCount ? Encoding.UTF8.GetString(bytes) : throw new EndOfStreamException();
	}

	public static void Write(BinaryWriter writer, CoreVariant variant)
	{
		writer.Write((byte)variant.Type);

		switch (variant.Type)
		{
			case CoreVariantType.Null:
				break;

			case CoreVariantType.Boolean:
				writer.Write((bool)variant);
				break;

			case CoreVariantType.Int64:
				writer.Write((long)variant);
				break;

			case CoreVariantType.Double when !double.IsFinite((double)variant):
				throw new InvalidOperationException("Saves support only finite floating-point values.");

			case CoreVariantType.Double:
				writer.Write((double)variant);
				break;

			case CoreVariantType.String:
				WriteString(writer, (string)variant, SaveValidation.MaxStringByteCount);
				break;

			default:
				throw new InvalidOperationException($"Unsupported variant type: {variant.Type}.");
		}
	}

	/// <summary>Writes a string like <see cref="BinaryWriter.Write(string)" />, up to a byte count.</summary>
	public static void WriteString(BinaryWriter writer, string value, int maxByteCount)
	{
		if (Encoding.UTF8.GetByteCount(value) > maxByteCount)
			throw new InvalidOperationException(
				string.Create(CultureInfo.InvariantCulture, $"String exceeds {maxByteCount} UTF-8 bytes."));

		writer.Write(value);
	}
}
