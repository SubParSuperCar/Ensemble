using System.Numerics;
using System.Text;
using Root.Core.Api.Asset;

namespace Root.Save;

// ReSharper disable once UnusedType.Global
public sealed class BinarySaveSerializer : ISaveSerializer
{
	private const ushort FormatVersion = 1;
	private static ReadOnlySpan<byte> Magic => "ENSB"u8;

	public void Serialize(Stream stream, SaveData data)
	{
		using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

		writer.Write(Magic);
		writer.Write(FormatVersion);

		writer.Write(data.Version);
		writer.Write(data.CreatedAt.UtcTicks);

		writer.Write(data.Instances.Count);

		foreach (var instance in data.Instances)
		{
			writer.Write(instance.AssetId);

			writer.Write(instance.Position.X);
			writer.Write(instance.Position.Y);
			writer.Write(instance.Position.Z);

			writer.Write(instance.Rotation.X);
			writer.Write(instance.Rotation.Y);
			writer.Write(instance.Rotation.Z);
			writer.Write(instance.Rotation.W);

			var properties = instance.Properties;
			writer.Write(properties?.Count ?? 0);

			if (properties is null)
				continue;

			foreach (var (key, value) in properties)
			{
				writer.Write(key);
				VariantSerializer.Write(writer, value);
			}
		}
	}

#pragma warning disable MA0051
	public SaveData Deserialize(Stream stream)
#pragma warning restore MA0051
	{
		using var reader = new BinaryReader(stream, Encoding.UTF8, true);

		var magic = reader.ReadBytes(Magic.Length);
		if (!magic.AsSpan().SequenceEqual(Magic))
			throw new InvalidDataException("Invalid save file.");

		var formatVersion = reader.ReadUInt16();
		if (formatVersion is not FormatVersion)
			throw new InvalidDataException(
				$"Unsupported save format version: {formatVersion}");

		var save = new SaveData
		{
			Version = reader.ReadUInt16(),
			CreatedAt = new DateTimeOffset(reader.ReadInt64(), TimeSpan.Zero)
		};

		// Signed counts make corruption easier to detect.
		// For example, FF FF FF FF becomes -1 instead of 4,294,967,295.
		var instanceCount = reader.ReadInt32();
		if (instanceCount is < 0 or > ushort.MaxValue)
			throw new InvalidDataException("Invalid instance count.");

		save.Instances.Capacity = instanceCount;

		for (var i = 0; i < instanceCount; i++)
		{
			var assetId = reader.ReadUInt16();

			var position = new Vector3(
				reader.ReadSingle(),
				reader.ReadSingle(),
				reader.ReadSingle());

			var rotation = new Quaternion(
				reader.ReadSingle(),
				reader.ReadSingle(),
				reader.ReadSingle(),
				reader.ReadSingle());

			var propertyCount = reader.ReadInt32();
			if (propertyCount is < 0 or > ushort.MaxValue)
				throw new InvalidDataException("Invalid property count.");

			Dictionary<string, Variant>? properties = null;

			if (propertyCount > 0)
			{
				properties = new Dictionary<string, Variant>(propertyCount, StringComparer.Ordinal);

				for (var j = 0; j < propertyCount; j++)
				{
					var key = reader.ReadString();
					var value = VariantSerializer.Read(reader);

					properties.Add(key, value);
				}
			}

			save.Instances.Add(new SaveInstance
			{
				AssetId = assetId,
				Position = position,
				Rotation = rotation,
				Properties = properties
			});
		}

		return save;
	}
}
