using Root.Core.Api.Asset;

namespace Root.Save;

internal static class VariantSerializer
{
	public static Variant Read(BinaryReader reader)
	{
		var type = (VariantType)reader.ReadByte();

		return type switch
		{
			VariantType.Null => Variant.Null,
			VariantType.Bool => new Variant(reader.ReadBoolean()),
			VariantType.NumInt => new Variant(reader.ReadInt64()),
			VariantType.NumDouble => new Variant(reader.ReadDouble()),
			VariantType.Str => new Variant(reader.ReadString()),
			_ => throw new InvalidDataException($"Unknown variant type: {type}")
		};
	}

	public static void Write(BinaryWriter writer, Variant variant)
	{
		writer.Write((byte)variant.Type);

		switch (variant.Type)
		{
			case VariantType.Null:
				break;

			case VariantType.Bool:
				writer.Write((bool)variant);
				break;

			case VariantType.NumInt:
				writer.Write((long)variant);
				break;

			case VariantType.NumDouble:
				writer.Write((double)variant);
				break;

			case VariantType.Str:
				writer.Write((string)variant);
				break;

			default:
				throw new InvalidOperationException($"Unsupported variant type: {variant.Type}");
		}
	}
}
