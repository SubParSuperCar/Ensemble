using CoreRoot.Api.Assets;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

namespace Root.Persistence;

internal static class VariantSerializer
{
	public static CoreVariant Read(BinaryReader reader)
	{
		var type = (CoreVariantType)reader.ReadByte();

		return type switch
		{
			CoreVariantType.Null => CoreVariant.Null,
			CoreVariantType.Bool => new CoreVariant(reader.ReadBoolean()),
			CoreVariantType.NumInt => new CoreVariant(reader.ReadInt64()),
			CoreVariantType.NumDouble => new CoreVariant(reader.ReadDouble()),
			CoreVariantType.Str => new CoreVariant(reader.ReadString()),
			_ => throw new InvalidDataException($"Unknown variant type: {type}")
		};
	}

	public static void Write(BinaryWriter writer, CoreVariant variant)
	{
		writer.Write((byte)variant.Type);

		switch (variant.Type)
		{
			case CoreVariantType.Null:
				break;

			case CoreVariantType.Bool:
				writer.Write((bool)variant);
				break;

			case CoreVariantType.NumInt:
				writer.Write((long)variant);
				break;

			case CoreVariantType.NumDouble:
				writer.Write((double)variant);
				break;

			case CoreVariantType.Str:
				writer.Write((string)variant);
				break;

			default:
				throw new InvalidOperationException($"Unsupported variant type: {variant.Type}");
		}
	}
}
