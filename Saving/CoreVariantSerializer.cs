using CoreRoot.Api.Assets;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

namespace Root.Saving;

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
			CoreVariantType.Double => new CoreVariant(reader.ReadDouble()),
			CoreVariantType.String => new CoreVariant(reader.ReadString()),
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

			case CoreVariantType.Boolean:
				writer.Write((bool)variant);
				break;

			case CoreVariantType.Int64:
				writer.Write((long)variant);
				break;

			case CoreVariantType.Double:
				writer.Write((double)variant);
				break;

			case CoreVariantType.String:
				writer.Write((string)variant);
				break;

			default:
				throw new InvalidOperationException($"Unsupported variant type: {variant.Type}");
		}
	}
}
