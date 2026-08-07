using System.Numerics;
using System.Text;

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

			writer.Write(instance.Properties?.Count ?? 0);
		}
	}

	public SaveData Deserialize(Stream stream)
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

		var instanceCount = reader.ReadInt32();
		if (instanceCount < 0)
			throw new InvalidDataException("Invalid instance count.");

		save.Instances.Capacity = instanceCount;

		for (var i = 0; i < instanceCount; i++)
		{
			var instance = new SaveInstance
			{
				AssetId = reader.ReadUInt16(),

				Position = new Vector3(
					reader.ReadSingle(),
					reader.ReadSingle(),
					reader.ReadSingle()),

				Rotation = new Quaternion(
					reader.ReadSingle(),
					reader.ReadSingle(),
					reader.ReadSingle(),
					reader.ReadSingle())
			};

			var propertyCount = reader.ReadInt32();
			if (propertyCount < 0)
				throw new InvalidDataException("Invalid property count.");

			save.Instances.Add(instance);
		}

		return save;
	}
}
